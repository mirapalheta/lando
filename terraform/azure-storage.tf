# ========================================
# Storage — Azure Storage account + Tailscale state file share
# ========================================
# The file share is mounted into the Tailscale gateway sidecar (see azure-app.tf)
# so it preserves its device identity across Container App restarts.

resource "azurerm_storage_account" "lando" {
  name                     = local.names.storage_account
  resource_group_name      = azurerm_resource_group.lando.name
  location                 = azurerm_resource_group.lando.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = var.tags

  lifecycle {
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
