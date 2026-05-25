# ========================================
# Container Registry — image storage + RBAC
# ========================================

resource "azurerm_container_registry" "lando" {
  name                = local.names.container_registry
  location            = azurerm_resource_group.lando.location
  resource_group_name = azurerm_resource_group.lando.name
  sku                 = "Basic"
  admin_enabled       = true
  tags                = var.tags
}

# ----------------------------------------
# RBAC role assignments
# ----------------------------------------

# Container App pulls images at deploy/scale time.
resource "azurerm_role_assignment" "container_app_acr_pull" {
  scope                = azurerm_container_registry.lando.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}

# GitHub Actions pushes new image tags from CI/CD.
resource "azurerm_role_assignment" "github_actions_acr_push" {
  scope                = azurerm_container_registry.lando.id
  role_definition_name = "AcrPush"
  principal_id         = azuread_service_principal.github_actions.object_id
}
