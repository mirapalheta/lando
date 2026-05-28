# ========================================
# Key Vault — secrets + RBAC
# ========================================
# When var.key_vault is provided (non-null), the vault is managed externally
# and lando skips creating its own.
#
# Standalone lando usage: leave var.key_vault null (default) and lando
# creates its own vault.

resource "azurerm_key_vault" "lando" {
  count                           = local.key_vault_count
  name                            = local.names.key_vault
  location                        = azurerm_resource_group.lando.location
  resource_group_name             = azurerm_resource_group.lando.name
  enabled_for_disk_encryption     = false
  enabled_for_template_deployment = true
  enabled_for_deployment          = true
  tenant_id                       = data.azurerm_client_config.current.tenant_id
  sku_name                        = "standard"
  purge_protection_enabled        = false
  rbac_authorization_enabled      = true
  tags                            = local.tags
}

locals {
  key_vault_count = var.key_vault == null ? 1 : 0
  key_vault = {
    id  = try(var.key_vault.id, azurerm_key_vault.lando[0].id)
    uri = try(var.key_vault.uri, azurerm_key_vault.lando[0].vault_uri)
  }
}

# ----------------------------------------
# Secrets — created only for values that were actually provided
# ----------------------------------------

resource "azurerm_key_vault_secret" "secrets" {
  for_each = toset(local.secrets.enabled)

  name         = each.value
  value        = local.secrets.values[each.value]
  key_vault_id = local.key_vault.id

  depends_on = [azurerm_role_assignment.terraform_keyvault_admin]
}

# ----------------------------------------
# RBAC role assignments
# ----------------------------------------

# Lets whoever runs `terraform apply` create/update secrets.
# Only needed when KV is managed locally.
resource "azurerm_role_assignment" "terraform_keyvault_admin" {
  count                = local.key_vault_count
  scope                = local.key_vault.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = data.azurerm_client_config.current.object_id

  lifecycle {
    # Pinning the principal to whoever first applied so the human's data-plane
    # access survives CI applies running as the SP.
    ignore_changes = [principal_id]
  }
}

# Mirrors terraform_keyvault_admin for the GitHub Actions SP — only when KV
# is local. When var.key_vault is provided, the grant is managed externally.
resource "azurerm_role_assignment" "github_actions_keyvault_secrets" {
  count                = max(local.github_actions_count, local.key_vault_count)
  scope                = local.key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = local.github_actions.id
}

# Container App reads secrets at startup via Key Vault references.
# Always granted in lando since the workload identity is managed here.
resource "azurerm_role_assignment" "container_app_key_vault_secrets_user" {
  scope                = local.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}

# Allows the Container App to write secrets at runtime (e.g. AcceptGrant flow).
resource "azurerm_role_assignment" "container_app_key_vault_secrets_officer" {
  scope                = local.key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}
