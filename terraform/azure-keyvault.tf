# ========================================
# Key Vault — secrets + RBAC
# ========================================
# Holds all secrets consumed by the Container App. RBAC role assignments
# co-located here so the answer to "who can read/write this vault?" is in
# one file.

resource "azurerm_key_vault" "lando" {
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
  tags                            = var.tags
}

# ----------------------------------------
# Secrets — created only for values that were actually provided
# ----------------------------------------

resource "azurerm_key_vault_secret" "secrets" {
  for_each = toset(local.secrets.enabled)

  name         = each.value
  value        = local.secrets.values[each.value]
  key_vault_id = azurerm_key_vault.lando.id

  depends_on = [azurerm_role_assignment.terraform_keyvault_admin]
}

# ----------------------------------------
# RBAC role assignments
# ----------------------------------------

# Lets whoever runs `terraform apply` create/update secrets.
resource "azurerm_role_assignment" "terraform_keyvault_admin" {
  scope                = azurerm_key_vault.lando.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = data.azurerm_client_config.current.object_id

  lifecycle {
    # Same rationale as azurerm_role_assignment.tfstate_admin in
    # azure-storage.tf — pinning the principal to whoever first applied
    # so the human's data-plane access to Key Vault secrets survives CI
    # applies running as the SP. The SP has its own grant via
    # github_actions_keyvault_secrets below.
    ignore_changes = [principal_id]
  }
}

# Mirrors terraform_keyvault_admin above for the GitHub Actions deploy SP
# (see azure-identity.tf). Required so CI's `terraform apply` can manage the
# secrets in azurerm_key_vault_secret.secrets. Scoped to "Secrets Officer"
# (secret CRUD) rather than Administrator (full vault + keys + certs) since
# terraform only touches secrets here — no certificates or cryptographic
# keys are managed via this stack.
resource "azurerm_role_assignment" "github_actions_keyvault_secrets" {
  scope                = azurerm_key_vault.lando.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = azuread_service_principal.github_actions.object_id
}

# Container App reads secrets at startup via the Key Vault references in
# its `secret` blocks.
resource "azurerm_role_assignment" "container_app_key_vault_secrets_user" {
  scope                = azurerm_key_vault.lando.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}

# Allows the container app to *write* secrets at runtime — needed so the
# AcceptGrant flow can persist per-grantee Alexa refresh tokens directly into
# Key Vault. Secrets User above grants read; Secrets Officer extends to
# create/update/delete.
resource "azurerm_role_assignment" "container_app_key_vault_secrets_officer" {
  scope                = azurerm_key_vault.lando.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = azurerm_user_assigned_identity.lando.principal_id

  depends_on = [azurerm_user_assigned_identity.lando]
}
