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
  service_principal_id = azuread_service_principal.github_actions.id
}

resource "azurerm_role_assignment" "github_actions_contributor" {
  scope                = azurerm_resource_group.lando.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
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
  principal_id         = azuread_service_principal.github_actions.object_id
}
