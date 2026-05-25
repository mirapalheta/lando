# ========================================
# Azure foundation — tenant lookup + resource group
# ========================================
# Everything else in the Azure stack lives inside this resource group.

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "lando" {
  name     = local.names.resource_group
  location = var.location
  tags     = var.tags
}
