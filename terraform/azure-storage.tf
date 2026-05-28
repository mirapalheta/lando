# ========================================
# Storage — app file shares (Tailscale state)
# ========================================
# When var.storage_account is provided (non-null), the storage account is
# managed externally and lando skips creating its own. The Tailscale file share
# is still created here (it's app-specific) but lives inside the shared account.
#
# Standalone lando usage: leave var.storage_account null (default) and lando
# creates its own storage account.
#
# The tfstate container and its RBAC below are retained for standalone mode only.

resource "azurerm_storage_account" "lando" {
  count                    = local.storage_account_count
  name                     = local.names.storage_account
  resource_group_name      = azurerm_resource_group.lando.name
  location                 = azurerm_resource_group.lando.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = local.tags

  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = false

  # Container Apps file-share mounts authenticate with the access key.
  shared_access_key_enabled = true

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
    prevent_destroy = true

    ignore_changes = [
      shared_access_key_enabled
    ]
  }
}

locals {
  storage_account_count = var.storage_account == null ? 1 : 0
  storage_account = {
    id                = nonsensitive(try(var.storage_account.id, azurerm_storage_account.lando[0].id))
    name              = nonsensitive(try(var.storage_account.name, azurerm_storage_account.lando[0].name))
    access_key        = try(var.storage_account.access_key, azurerm_storage_account.lando[0].primary_access_key)
    connection_string = try(var.storage_account.connection_string, azurerm_storage_account.lando[0].primary_connection_string)
  }
  tfstate_container_id = try(var.storage_account.tfstate_container_id, azurerm_storage_container.tfstate[0].id)
}

# Tailscale gateway sidecar persists device identity across Container App
# restarts using this file share.
resource "azurerm_storage_share" "tailscale_state" {
  name               = "tailscale-state"
  storage_account_id = local.storage_account.id
  quota              = 1 # 1GB is more than enough for Tailscale state files

  depends_on = [azurerm_storage_account.lando]
}

# ----------------------------------------
# Standalone-mode Terraform state resources
# ----------------------------------------
# The container and RBAC below are only created when lando manages its own
# storage account (standalone mode). When the account is provided externally,
# Terraform state is managed outside this module and these are skipped.

resource "azurerm_storage_container" "tfstate" {
  count                 = local.storage_account_count
  name                  = "tfstate"
  storage_account_id    = local.storage_account.id
  container_access_type = "private"

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_role_assignment" "tfstate_admin" {
  count                = local.storage_account_count
  scope                = local.tfstate_container_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id

  description = "Allows the Terraform-running identity to read and write the Lando state blob over Entra ID auth (use_azuread_auth)."

  lifecycle {
    ignore_changes = [principal_id]
  }
}

resource "azurerm_role_assignment" "github_actions_tfstate_data" {
  count                = max(local.github_actions_count, local.storage_account_count)
  scope                = local.tfstate_container_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = local.github_actions.id

  description = "Allows the GitHub Actions deploy SP to read and write the Lando state blob over Entra ID auth (use_azuread_auth)."
}
