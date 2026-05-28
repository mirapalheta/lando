# ========================================
# Azure foundation — tenant lookup + resource group
# ========================================
# Everything else in the Azure stack lives inside this resource group.

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "lando" {
  name     = local.names.resource_group
  location = var.location
  tags     = local.tags
}

resource "azurerm_role_assignment" "github_actions_contributor" {
  scope                = azurerm_resource_group.lando.id
  role_definition_name = "Contributor"
  principal_id         = local.github_actions.id
}

# Several resources in this stack create role assignments (the tfstate blob
# admin, the GH Actions data-plane roles on storage and Key Vault, the
# Container App's identity grants, etc.). Contributor above covers
# Microsoft.Resources/* but NOT Microsoft.Authorization/* — role assignment
# management is a separate privilege class. Without this, `terraform apply`
# from CI fails 403 on any refresh that needs to touch a roleAssignments
# resource (typically the delete-and-recreate dance for property changes).
#
# This is the SP equivalent of what the README documents for the human
# identity: "needs User Access Administrator or Owner on the subscription".
# Scoped to the RG here rather than the subscription so the SP's reach
# stops at the Lando footprint.
#
# Trade-off: User Access Administrator on the RG means the SP can grant
# itself additional roles within the RG (escalation path). Acceptable for a
# personal-project SP that's already trusted with the rest of the stack;
# would not be acceptable for a multi-tenant or compliance-bound setup.
resource "azurerm_role_assignment" "github_actions_user_access_admin" {
  scope                = azurerm_resource_group.lando.id
  role_definition_name = "User Access Administrator"
  principal_id         = local.github_actions.id
}
