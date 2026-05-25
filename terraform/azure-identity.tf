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
  tags                = var.tags
}

# ----------------------------------------
# GitHub Actions service principal for CI/CD
# ----------------------------------------

resource "azuread_application" "github_actions" {
  display_name = "sp-${var.project_name}-github-actions"
}

resource "azuread_service_principal" "github_actions" {
  client_id = azuread_application.github_actions.client_id
  owners    = [data.azurerm_client_config.current.object_id]
}

resource "azuread_service_principal_password" "github_actions" {
  service_principal_id = azuread_service_principal.github_actions.id
}

resource "azurerm_role_assignment" "github_actions_contributor" {
  scope                = azurerm_resource_group.lando.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
}
