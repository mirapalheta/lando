# ========================================
# Monitoring & diagnostics
# ========================================
# Log Analytics → App Insights → Action Group → Smart Detector alerts.
# Diagnostic Settings ship Container App + Key Vault telemetry to LAW.

# ----------------------------------------
# Log Analytics + Application Insights
# ----------------------------------------

resource "azurerm_log_analytics_workspace" "lando" {
  name                = local.names.log_analytics_workspace
  location            = azurerm_resource_group.lando.location
  resource_group_name = azurerm_resource_group.lando.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_application_insights" "lando" {
  name                = local.names.app_insights
  location            = azurerm_resource_group.lando.location
  resource_group_name = azurerm_resource_group.lando.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.lando.id
  tags                = var.tags
}

# ----------------------------------------
# Action group — where alerts land
# ----------------------------------------

resource "azurerm_monitor_action_group" "lando" {
  name                = local.names.monitor_action_group
  resource_group_name = azurerm_resource_group.lando.name
  short_name          = local.names.monitor_action_group_short
  tags                = var.tags
}

# ----------------------------------------
# Diagnostic Settings
# ----------------------------------------

resource "azurerm_monitor_diagnostic_setting" "container_app" {
  name                       = local.names.diagnostic_settings
  target_resource_id         = azurerm_container_app.lando.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.lando.id

  enabled_metric {
    category = "AllMetrics"
  }
}

resource "azurerm_monitor_diagnostic_setting" "key_vault" {
  name                       = "${local.names.diagnostic_settings}-kv"
  target_resource_id         = azurerm_key_vault.lando.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.lando.id

  enabled_log {
    category = "AuditEvent"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}

# ----------------------------------------
# Smart-detector alert rules
# ----------------------------------------
# Requires Microsoft.AlertsManagement namespace to be registered on the subscription
# Register with: az provider register --namespace Microsoft.AlertsManagement
# Automatically creates alert rules for each detector in local.smart_detectors.
resource "azurerm_monitor_smart_detector_alert_rule" "all_detectors" {
  for_each = local.smart_detectors

  # Resolves human-readable formatting matching naming conventions
  # Example: failure_anomalies → "Failure Anomalies - lando"
  name                = "${title(replace(each.key, "_", " "))} - ${var.project_name}"
  resource_group_name = azurerm_resource_group.lando.name
  severity            = "Sev3"
  enabled             = true
  frequency           = each.value.frequency
  scope_resource_ids  = [azurerm_application_insights.lando.id]
  detector_type       = each.value.type

  action_group {
    ids = [azurerm_monitor_action_group.lando.id]
  }
}
