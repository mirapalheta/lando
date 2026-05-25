output "resource_group_id" {
  description = "The ID of the created resource group"
  value       = azurerm_resource_group.lando.id
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = azurerm_resource_group.lando.name
}

output "storage_account_id" {
  description = "The ID of the storage account"
  value       = azurerm_storage_account.lando.id
}

output "storage_account_name" {
  description = "The name of the storage account"
  value       = azurerm_storage_account.lando.name
}

output "key_vault_id" {
  description = "The ID of the Key Vault"
  value       = azurerm_key_vault.lando.id
}

output "key_vault_uri" {
  description = "The URI of the Key Vault"
  value       = azurerm_key_vault.lando.vault_uri
}

output "app_insights_id" {
  description = "The ID of Application Insights"
  value       = azurerm_application_insights.lando.id
}

output "app_insights_instrumentation_key" {
  description = "The instrumentation key for Application Insights"
  value       = azurerm_application_insights.lando.instrumentation_key
  sensitive   = true
}

output "log_analytics_workspace_id" {
  description = "The ID of the Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.lando.id
}

output "log_analytics_workspace_name" {
  description = "The name of the Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.lando.name
}

output "action_group_id" {
  description = "The ID of the Monitor Action Group"
  value       = azurerm_monitor_action_group.lando.id
}

output "action_group_name" {
  description = "The name of the Monitor Action Group"
  value       = azurerm_monitor_action_group.lando.name
}

output "container_app_id" {
  description = "The ID of the Container App"
  value       = azurerm_container_app.lando.id
}

output "container_app_name" {
  description = "The name of the Container App"
  value       = azurerm_container_app.lando.name
}

output "container_app_latest_revision_name" {
  description = "The name of the latest revision of the Container App"
  value       = azurerm_container_app.lando.latest_revision_name
}

output "container_app_url" {
  description = "The FQDN of the Container App"
  value       = azurerm_container_app.lando.ingress[0].fqdn
}

output "user_assigned_identity_id" {
  description = "The ID of the User-Assigned Identity"
  value       = azurerm_user_assigned_identity.lando.id
}

output "user_assigned_identity_principal_id" {
  description = "The principal ID of the User-Assigned Identity"
  value       = azurerm_user_assigned_identity.lando.principal_id
}

output "container_app_principal_id" {
  description = "The principal ID of the Container App's managed identity"
  value       = azurerm_container_app.lando.identity[0].principal_id
}

output "container_app_environment_id" {
  description = "The ID of the Container App Environment"
  value       = azurerm_container_app_environment.lando.id
}

output "container_registry_name" {
  description = "The name of the Azure Container Registry"
  value       = azurerm_container_registry.lando.name
}

output "container_registry_login_server" {
  description = "The login server of the Azure Container Registry"
  value       = azurerm_container_registry.lando.login_server
}

output "container_registry_id" {
  description = "The ID of the Azure Container Registry"
  value       = azurerm_container_registry.lando.id
}

output "azure_subscription_id" {
  description = "Azure subscription ID"
  value       = data.azurerm_client_config.current.subscription_id
}

output "azure_tenant_id" {
  description = "Azure tenant ID"
  value       = data.azurerm_client_config.current.tenant_id
}

output "github_actions_client_id" {
  description = "Client ID for GitHub Actions service principal"
  value       = azuread_application.github_actions.client_id
}

output "github_actions_client_secret" {
  description = "Client secret for GitHub Actions service principal"
  value       = azuread_service_principal_password.github_actions.value
  sensitive   = true
}

output "github_actions_service_principal_object_id" {
  description = "Object ID of the GitHub Actions service principal"
  value       = azuread_service_principal.github_actions.object_id
}

output "container_registry_url" {
  description = "The login server URL for the container registry"
  value       = azurerm_container_registry.lando.login_server
}

output "container_registry_username" {
  description = "The admin username for the container registry"
  value       = azurerm_container_registry.lando.admin_username
}

output "container_registry_password" {
  description = "The admin password for the container registry"
  value       = azurerm_container_registry.lando.admin_password
  sensitive   = true
}

output "diagnostic_setting_container_app_id" {
  description = "The ID of the Container App diagnostic setting"
  value       = azurerm_monitor_diagnostic_setting.container_app.id
}

output "diagnostic_setting_key_vault_id" {
  description = "The ID of the Key Vault diagnostic setting"
  value       = azurerm_monitor_diagnostic_setting.key_vault.id
}

# output "smart_detector_alert_rule_id" {
#   description = "The ID of the smart detector alert rule for failure anomalies"
#   value       = azurerm_monitor_smart_detector_alert_rule.failure_anomalies.id
# }

# ========================================
# AWS / Alexa Lambda outputs
# ========================================

output "alexa_smart_home_arn" {
  description = "ARN of the Alexa Smart Home proxy Lambda."
  value       = aws_lambda_function.alexa_smart_home.arn
}

output "alexa_smart_home_function_name" {
  description = "Name of the Alexa Smart Home proxy Lambda — paste into the Alexa Developer Console under Smart Home → Default endpoint."
  value       = aws_lambda_function.alexa_smart_home.function_name
}

output "hmac_shared_secret_aws_arn" {
  description = "ARN of the HMAC shared secret in AWS Secrets Manager."
  value       = aws_secretsmanager_secret.hmac_shared_secret.arn
}

output "aws_account_id" {
  description = "AWS account hosting the Alexa Lambda."
  value       = data.aws_caller_identity.current.account_id
}
