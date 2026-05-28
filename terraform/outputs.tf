# ============================================================================
# Azure foundation
# ============================================================================
# Resource group + storage account. The storage account doubles as the
# Terraform backend (tfstate container lives inside it).

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
  value       = local.storage_account.id
  sensitive   = true
}

output "storage_account_name" {
  description = "Name of the app storage account"
  value       = local.storage_account.name
  sensitive   = true
}

# ============================================================================
# Azure security & observability
# ============================================================================
# Key Vault, Application Insights, Log Analytics, Monitor action group.

output "key_vault_id" {
  description = "The ID of the Key Vault"
  value       = local.key_vault.id
}

output "key_vault_uri" {
  description = "Vault URI of the Key Vault"
  value       = local.key_vault.uri
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

# ============================================================================
# Azure diagnostic settings
# ============================================================================

output "diagnostic_setting_container_app_id" {
  description = "The ID of the Container App diagnostic setting"
  value       = azurerm_monitor_diagnostic_setting.container_app.id
}

output "diagnostic_setting_key_vault_id" {
  description = "The ID of the Key Vault diagnostic setting (null when KV is externally managed)"
  value       = try(azurerm_monitor_diagnostic_setting.key_vault[0].id, null)
}

# output "smart_detector_alert_rule_id" {
#   description = "The ID of the smart detector alert rule for failure anomalies"
#   value       = azurerm_monitor_smart_detector_alert_rule.failure_anomalies.id
# }

# ============================================================================
# Azure Container App
# ============================================================================
# Container App + its environment + the workload identity it runs as.

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

output "container_app_environment_id" {
  description = "The ID of the Container App Environment"
  value       = azurerm_container_app_environment.lando.id
}

output "container_app_principal_id" {
  description = "The principal ID of the Container App's managed identity"
  value       = azurerm_container_app.lando.identity[0].principal_id
}

output "user_assigned_identity_id" {
  description = "The ID of the User-Assigned Identity"
  value       = azurerm_user_assigned_identity.lando.id
}

output "user_assigned_identity_principal_id" {
  description = "The principal ID of the User-Assigned Identity"
  value       = azurerm_user_assigned_identity.lando.principal_id
}

# ============================================================================
# Azure Container Registry
# ============================================================================
# Terraform's image build uses `az acr login` during `null_resource.acr_image_build`,
# so the admin credentials below aren't required by the deploy workflow — kept
# around for ad-hoc docker / push-pull tooling.

output "container_registry_id" {
  description = "The ID of the Azure Container Registry"
  value       = local.container_registry.id
}

output "container_registry_name" {
  description = "The name of the Azure Container Registry"
  value       = local.container_registry.name
}

output "container_registry_login_server" {
  description = "The login server of the Azure Container Registry"
  value       = local.container_registry.login_server
}

output "container_registry_url" {
  description = "The login server URL for the container registry"
  value       = local.container_registry.login_server
}

output "container_registry_username" {
  description = "The admin username for the container registry"
  value       = local.container_registry.admin_username
}

output "container_registry_password" {
  description = "The admin password for the container registry"
  value       = local.container_registry.admin_password
  sensitive   = true
}

output "container_app_image_tag" {
  description = "Effective image tag deployed to the Container App — captured from var.app_version the last time source files changed and persisted via terraform_data.image_tag. Diverges from var.app_version whenever the tag was bumped without a code change."
  value       = terraform_data.image_tag.output
}

# ============================================================================
# Azure tenancy + GitHub Actions service principal
# ============================================================================
# Push into GitHub as AZURE_CLIENT_ID / AZURE_CLIENT_SECRET / AZURE_TENANT_ID /
# AZURE_SUBSCRIPTION_ID for the azure/login@v3 step in the workflow.
# See deployment.md § "Set GitHub Secrets and Variables" for the full command list.

output "azure_subscription_id" {
  description = "Azure subscription ID"
  value       = data.azurerm_client_config.current.subscription_id
}

output "azure_tenant_id" {
  description = "Azure tenant ID"
  value       = data.azurerm_client_config.current.tenant_id
}

output "github_actions_client_id" {
  description = "Client ID for GitHub Actions service principal. Empty when github_actions_identity_id is provided (identity is managed externally)."
  value       = local.github_actions.client_id
}

output "github_actions_client_secret" {
  description = "Client secret for GitHub Actions service principal. Empty when github_actions_identity_id is provided (password is managed externally)."
  value       = local.github_actions.client_secret
  sensitive   = true
}

output "github_actions_service_principal_object_id" {
  description = "Object ID of the GitHub Actions service principal (local or external)."
  value       = local.github_actions.id
}

# ============================================================================
# AWS / Alexa Smart Home Lambda
# ============================================================================

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

# ============================================================================
# AWS GitHub Actions IAM deploy user
# ============================================================================
# Push into GitHub as AWS_ACCESS_KEY_ID + AWS_SECRET_ACCESS_KEY secrets.
# Mirrors the Azure SP pattern (github_actions_client_id / github_actions_client_secret).
# See deployment.md § "Set GitHub Secrets and Variables" for the full command list.

output "aws_github_actions_access_key_id" {
  description = "Access key ID for the GitHub Actions IAM user (user-{project}-github-actions)."
  value       = aws_iam_access_key.github_actions.id
}

output "aws_github_actions_access_key_secret" {
  description = "Secret access key for the GitHub Actions IAM user. Only visible at create time; rotate by tainting aws_iam_access_key.github_actions."
  value       = aws_iam_access_key.github_actions.secret
  sensitive   = true
}

output "aws_region" {
  description = "AWS region the Lambda + Secrets Manager secret live in. Pushed into GH as AWS_REGION."
  value       = var.aws_region
}

# ============================================================================
# Passthrough — terraform.tfvars values mirrored to GitHub Actions
# ============================================================================
# These don't represent any TF-managed resource; they relay terraform.tfvars
# values so they can be pushed into GitHub in one `terraform output` pass.
# Source of truth remains in terraform.tfvars.
#
# The HA CA/cert outputs are read with file() so they expose the PEM
# *content* rather than the on-disk path. Path-based outputs would flip
# between local and CI because the same cert lives under different paths
# on each host (~/source/.../certs/foo.pem locally vs /home/runner/.../
# in GitHub Actions); content-based outputs are stable as long as the
# bytes are identical. Empty string when the corresponding var is unset.

output "home_assistant_base_url" {
  description = "Home Assistant base URL passthrough. Pushed into GH as HOME_ASSISTANT_BASE_URL."
  value       = var.home_assistant_base_url
}

output "home_assistant_token" {
  description = "Home Assistant long-lived access token passthrough. Pushed into GH as HOME_ASSISTANT_TOKEN."
  value       = var.home_assistant_token
  sensitive   = true
}

output "home_assistant_ca" {
  description = "Home Assistant CA PEM content (build-time). Empty if var.home_assistant_ca_file is unset. Pushed into GH as HOME_ASSISTANT_CA."
  value       = try(file(var.home_assistant_ca_file), "")
  sensitive   = true
}

output "home_assistant_cert" {
  description = "Home Assistant host certificate PEM content (build-time). Empty if var.home_assistant_cert_file is unset. Pushed into GH as HOME_ASSISTANT_CERT."
  value       = try(file(var.home_assistant_cert_file), "")
  sensitive   = true
}

output "tailscale_auth_key" {
  description = "Tailscale auth key passthrough. Pushed into GH as TAILSCALE_AUTH_KEY."
  value       = var.tailscale_auth_key
  sensitive   = true
}

output "alexa_smart_home_skill_id" {
  description = "Alexa Smart Home skill ID passthrough. Pushed into GH as ALEXA_SMART_HOME_SKILL_ID."
  value       = var.alexa_smart_home_skill_id
}

output "alexa_smart_home_auth_client_id" {
  description = "Login-with-Amazon client ID for inbound bearer-token validation. Pushed into GH as ALEXA_AUTH_CLIENT_ID."
  value       = var.alexa_smart_home_auth_client_id
}

output "alexa_smart_home_auth_client_secret" {
  description = "Login-with-Amazon client secret paired with alexa_smart_home_auth_client_id. Pushed into GH as ALEXA_AUTH_CLIENT_SECRET."
  value       = var.alexa_smart_home_auth_client_secret
  sensitive   = true
}

output "alexa_smart_home_event_client_id" {
  description = "Login-with-Amazon client ID for the Alexa Event Gateway. Pushed into GH as ALEXA_EVENT_CLIENT_ID."
  value       = var.alexa_smart_home_event_client_id
}

output "alexa_smart_home_event_client_secret" {
  description = "Login-with-Amazon client secret paired with alexa_smart_home_event_client_id. Pushed into GH as ALEXA_EVENT_CLIENT_SECRET."
  value       = var.alexa_smart_home_event_client_secret
  sensitive   = true
}
