# ========================================
# Storage — Azure Storage account, Tailscale state file share, Terraform state container
# ========================================
# This storage account hosts two things:
#   1. The Tailscale state file share — mounted into the Tailscale gateway sidecar
#      (see azure-app.tf) so it preserves device identity across Container App restarts.
#   2. The Terraform state blob — see "tfstate" container below and the
#      backend "azurerm" block in provider.tf.
#
# Because the Tailscale file-share mount authenticates with the storage account
# access key (Container Apps file-share mounts don't support managed identity),
# shared_access_key_enabled MUST remain true. The Terraform backend itself
# authenticates via Entra ID (use_azuread_auth = true in provider.tf) so it
# never touches the shared key.

resource "azurerm_storage_account" "lando" {
  name                     = local.names.storage_account
  resource_group_name      = azurerm_resource_group.lando.name
  location                 = azurerm_resource_group.lando.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = var.tags

  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = false

  # Blob versioning + soft delete protect the Terraform state blob against
  # corruption, accidental deletion, or a bad `terraform apply` overwriting
  # state with garbage. 30 days is a reasonable recovery window.
  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 30
    }

    container_delete_retention_policy {
      days = 30
    }
  }

  lifecycle {
    # Destroying this storage account would also destroy the Terraform state
    # blob — i.e. Terraform's own memory of the rest of the stack. Removing
    # this guard requires a deliberate two-step (comment out, plan, apply).
    prevent_destroy = true

    ignore_changes = [
      shared_access_key_enabled
    ]
  }
}

resource "azurerm_storage_share" "tailscale_state" {
  name               = "tailscale-state"
  storage_account_id = azurerm_storage_account.lando.id
  quota              = 1 # 1GB is more than enough for Tailscale state files

  depends_on = [azurerm_storage_account.lando]
}

# Container that holds the Terraform state blob. Referenced from
# backend.hcl as container_name = "tfstate".
resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.lando.id
  container_access_type = "private"

  lifecycle {
    prevent_destroy = true
  }
}

# Grants the identity running Terraform (whoever is `az login`'d, or the
# service principal in CI) write access to the state container. Without this,
# `terraform init -migrate-state` would fail with a 403 on the first push to
# the remote backend because the storage account has shared_access_key_enabled
# pinned via ignore_changes — the backend authenticates via Entra ID only.
#
# Requires the principal applying this stack to hold `User Access Administrator`
# or `Owner` on the subscription (in addition to the Contributor rights needed
# to create everything else). Personal Azure accounts have this by default;
# corporate subscriptions may not.
resource "azurerm_role_assignment" "tfstate_admin" {
  scope                = azurerm_storage_container.tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id

  description = "Allows the Terraform-running identity to read and write the Lando state blob over Entra ID auth (use_azuread_auth)."

  lifecycle {
    # principal_id resolves to "whoever is running `terraform apply` right
    # now" — which alternates between the human (local apply) and the
    # GitHub Actions SP (CI apply). Without ignore_changes, each apply
    # would delete-and-recreate this assignment to flip it to the new
    # principal, locking out the identity that didn't apply last.
    #
    # By ignoring drift on principal_id we lock in whoever bootstrapped
    # the stack (the human's first apply), and let the SP get its own
    # separate grant via github_actions_tfstate_data below. Net result:
    # both identities keep their access permanently across alternating
    # applies.
    ignore_changes = [principal_id]
  }
}

# Mirrors tfstate_admin above, but for the GitHub Actions service principal
# (see azure-identity.tf). Required so the CI deploy can read and write the
# state blob via the Entra ID auth path. Without this, `terraform init` from
# CI fails with 403 AuthorizationPermissionMismatch on the first blob listing.
#
# The github_actions SP already has Contributor on the resource group
# (control-plane), but that role doesn't grant data-plane permissions on
# blob containers. Storage Blob Data Contributor is the data-plane equivalent.
resource "azurerm_role_assignment" "github_actions_tfstate_data" {
  scope                = azurerm_storage_container.tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id

  description = "Allows the GitHub Actions deploy SP to read and write the Lando state blob over Entra ID auth (use_azuread_auth)."
}
