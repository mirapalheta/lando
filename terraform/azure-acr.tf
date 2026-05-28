# ========================================
# Container Registry — image storage + RBAC
# ========================================
# When var.container_registry is provided (non-null), the registry is managed
# externally and lando skips creating its own.
#
# Standalone lando usage: leave var.container_registry null (default) and lando
# creates its own ACR.

resource "azurerm_container_registry" "lando" {
  count               = local.container_registry_count
  name                = local.names.container_registry
  location            = azurerm_resource_group.lando.location
  resource_group_name = azurerm_resource_group.lando.name
  sku                 = "Basic"
  admin_enabled       = true
  tags                = local.tags
}

locals {
  container_registry_count = var.container_registry == null ? 1 : 0
  container_registry = {
    id             = nonsensitive(try(var.container_registry.id, azurerm_container_registry.lando[0].id))
    name           = nonsensitive(try(var.container_registry.name, azurerm_container_registry.lando[0].name))
    login_server   = nonsensitive(try(var.container_registry.login_server, azurerm_container_registry.lando[0].login_server))
    admin_username = nonsensitive(try(var.container_registry.admin_username, azurerm_container_registry.lando[0].admin_username))
    admin_password = try(var.container_registry.admin_password, azurerm_container_registry.lando[0].admin_password)
  }
}

# ----------------------------------------
# RBAC role assignments
# ----------------------------------------

# Container App pulls images at deploy/scale time.
# Always granted in lando since the workload identity is managed here.
resource "azurerm_role_assignment" "container_app_acr_pull" {
  scope                = local.container_registry.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}

# AcrPush for the GitHub Actions SP — only when ACR is managed locally.
# When var.container_registry and the identity are provided,
# the AcrPush grant is managed externally.
resource "azurerm_role_assignment" "github_actions_acr_push" {
  count                = max(local.github_actions_count, local.container_registry_count)
  scope                = local.container_registry.id
  role_definition_name = "AcrPush"
  principal_id         = local.github_actions.id
}
