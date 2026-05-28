# ========================================
# Identities — workload identity + GitHub Actions SP
# ========================================
# RBAC role assignments for these identities live next to the resources they
# protect (Key Vault, ACR, etc.). The one exception is the GitHub Actions
# Contributor role at the resource-group scope, which is co-located here
# because the resource being protected (the resource group) doesn't have a
# dedicated file.

# ----------------------------------------
# Workload identity used by the Container App
# ----------------------------------------

resource "azurerm_user_assigned_identity" "lando" {
  name                = local.names.user_assigned_identity
  resource_group_name = azurerm_resource_group.lando.name
  location            = azurerm_resource_group.lando.location
  tags                = local.tags
}

locals {
  github_actions_count = var.github_actions_identity_id == "" ? 1 : 0
  github_actions = {
    id            = try(azuread_service_principal.github_actions[0].object_id, var.github_actions_identity_id)
    client_id     = try(azuread_application.github_actions[0].client_id, "")
    client_secret = try(azuread_service_principal_password.github_actions[0].value, "")
  }
}

# ----------------------------------------
# GitHub Actions service principal for CI/CD
# ----------------------------------------
# When var.github_actions_identity_id is provided (non-empty), these three
# resources are skipped — the caller (terraform/identity.tf) manages the SP
# and passes its object_id here. This keeps lando/terraform fully functional
# as a standalone root: leave github_actions_identity_id empty and it creates
# its own dedicated sp-lando-github-actions SP.

resource "azuread_application" "github_actions" {
  count        = local.github_actions_count
  display_name = "sp-${var.project_name}-github-actions"
}

resource "azuread_service_principal" "github_actions" {
  count     = local.github_actions_count
  client_id = azuread_application.github_actions[0].client_id
  owners    = [data.azurerm_client_config.current.object_id]

  lifecycle {
    # Same flip problem as the role assignments in azure-storage.tf and
    # azure-keyvault.tf: `owners` resolves to whichever identity is
    # currently running terraform, but the SP doesn't have Microsoft Graph
    # permissions to update its own service principal — that's a directory
    # operation, not Azure RBAC. ignore_changes keeps the owner pinned to
    # whoever bootstrapped the SP (the human), so CI applies don't try to
    # rewrite the owners list and get 403'd by Graph.
    ignore_changes = [owners]
  }
}

resource "azuread_service_principal_password" "github_actions" {
  count                = local.github_actions_count
  service_principal_id = azuread_service_principal.github_actions[0].id
}
