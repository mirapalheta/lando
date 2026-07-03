locals {
  # Resource naming.
  #
  # Azure side follows Cloud Adoption Framework abbreviations:
  #   https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations
  #
  # AWS side uses resource-type prefixes (lambda-, role-, policy-) — there is
  # no official AWS naming convention as opinionated as Azure CAF, so we
  # invent one that mirrors the Azure style. Region is intentionally omitted
  # from AWS names because AWS resources are already region-scoped by ARN.
  names = {
    # ---- Azure ----
    resource_group             = "rg-${var.project_name}-${var.location}"
    storage_account            = "st${var.project_name}${replace(var.location, "-", "")}"
    key_vault                  = "kv-${var.project_name}-${var.location}"
    app_insights               = "appi-${var.project_name}-${var.location}"
    container_registry         = "acr${var.project_name}${replace(var.location, "-", "")}"
    container_app              = "ca-${var.project_name}-${var.location}"
    container_app_environment  = "cae-${var.project_name}-${var.location}"
    log_analytics_workspace    = "law-${var.project_name}-${var.location}"
    monitor_action_group       = "ag-${var.project_name}-global"
    monitor_action_group_short = "ag-${var.project_name}"
    user_assigned_identity     = "uai-${var.project_name}-${var.location}"
    diagnostic_settings        = "diag-${var.project_name}"

    # ---- AWS ----
    lambda_alexa_proxy   = "lambda-${var.project_name}-alexa-proxy"
    iam_role_alexa_proxy = "role-${var.project_name}-alexa-proxy"
    iam_policy_hmac_read = "policy-${var.project_name}-hmac-secret-read"
    # Secrets Manager uses `/` as a hierarchy separator in the console;
    # keeping that style makes the secret group cleanly under "lando/".
    secretsmanager_hmac = "${var.project_name}/hmac/shared-secret"
  }

  settings = {
    app = {
      ASPNETCORE_ENVIRONMENT = "Production"
      WEBSITES_PORT          = tostring(var.app_port)
      ASPNETCORE_URLS        = "http://+:${var.app_port}"

      # Alexa skill configuration
      Alexa__SmartHome__Event__ClientId         = var.alexa_smart_home_event_client_id
      Alexa__SmartHome__Authorization__ClientId = var.alexa_smart_home_auth_client_id

      # Home Assistant client configuration (nested under ClientOptions)
      HomeAssistant__ClientOptions__BaseUrl             = var.home_assistant_base_url
      HomeAssistant__ClientOptions__ProxyAddress        = "socks5://localhost:${var.socks5_port}"
      HomeAssistant__ClientOptions__ProxyHealthCheckUrl = "http://localhost:${var.proxy_health_check_port}/healthz"
      HomeAssistant__ClientOptions__Certificate         = "" # Optional: base64-encoded custom CA certificate for HTTPS validation
    }

    gateway = {
      TS_ACCEPT_DNS          = "true"
      TS_ACCEPT_ROUTES       = "true"
      TS_AUTH_ONCE           = "true"
      TS_ENABLE_HEALTH_CHECK = "true"
      TS_ENABLE_METRICS      = "true"
      TS_HOSTNAME            = var.hostname == "container_app" ? local.names.container_app : var.hostname == "project_name" ? var.project_name : var.hostname
      TS_LOCAL_ADDR_PORT     = "[::]:${var.proxy_health_check_port}"
      TS_SOCKS5_SERVER       = ":${var.socks5_port}"
      TS_USERSPACE           = "true"
    }
  }

  # Secrets configuration - define names first
  secret_names_map = {
    AcrAdminPassword                = "acr-admin-password"
    TailscaleAuthKey                = "tailscale-authkey"
    HomeAssistantToken              = "home-assistant-token"
    HomeAssistantCertificate        = "home-assistant-certificate"
    StorageConnectionString         = "storage-connection-string"
    AppInsightsConnectionString     = "applicationinsights-connection-string"
    AlexaSmartHomeAuthClientSecret  = "alexa-smarthome-auth-client-secret"
    AlexaSmartHomeEventClientSecret = "alexa-smarthome-event-client-secret"
    HmacSharedSecret                = "hmac-shared-secret"
  }

  # Secrets configuration - only include those with values provided
  secrets = {
    names = local.secret_names_map
    enabled = nonsensitive(concat(
      [
        local.secret_names_map.TailscaleAuthKey,
        local.secret_names_map.HomeAssistantToken,
        local.secret_names_map.AlexaSmartHomeAuthClientSecret,
        local.secret_names_map.AlexaSmartHomeEventClientSecret,
        local.secret_names_map.HmacSharedSecret
      ],
      var.home_assistant_certificate != "" ? [local.secret_names_map.HomeAssistantCertificate] : []
    ))
    values = {
      "tailscale-authkey"                   = var.tailscale_auth_key
      "home-assistant-token"                = var.home_assistant_token
      "home-assistant-certificate"          = var.home_assistant_certificate
      "alexa-smarthome-auth-client-secret"  = var.alexa_smart_home_auth_client_secret
      "alexa-smarthome-event-client-secret" = var.alexa_smart_home_event_client_secret
      "hmac-shared-secret"                  = random_password.hmac_shared_secret.result
    }
  }

  # Smart Detection Alert Rule - Failure Anomalies
  # Requires Microsoft.AlertsManagement namespace to be registered on the subscription
  # Register with: az provider register --namespace Microsoft.AlertsManagement
  #
  # Map of all production-recommended built-in smart detectors.
  # Key = logical identifier, Value = exact Azure API detector string.
  # Comment out detectors to disable them without deleting the configuration.
  smart_detectors = {
    # CRITICAL: Detect failure rate anomalies (sudden increases in errors)
    failure_anomalies = {
      type      = "FailureAnomaliesDetector"
      frequency = "PT1M"
    }
    # HIGH: Detect anomalies in HTTP request response times (app latency)
    request_performance = {
      type      = "RequestPerformanceDegradationDetector"
      frequency = "P1D"
    }
    # HIGH: Detect anomalies in dependency call latencies (HA API, Tailscale proxy)
    dependency_performance = {
      type      = "DependencyPerformanceDegradationDetector"
      frequency = "P1D"
    }
    # MEDIUM: Detect memory leaks in the .NET app
    memory_leak = {
      type      = "MemoryLeakDetector"
      frequency = "P1D"
    }
    # MEDIUM: Detect unusual spikes in exception volumes
    exception_volume = {
      type      = "ExceptionVolumeChangedDetector"
      frequency = "P1D"
    }
  }

  # AWS Lambda packaging paths — consumed by aws-lambda-build.tf and aws-lambda.tf.
  #   source_dir → npm package root (cwd for `npm ci && npm run build`; also the
  #                directory whose source files determine the build hash)
  #   zip_path   → final packaged Lambda bundle. Kept MODULE-RELATIVE so the value
  #                used as aws_lambda_function.alexa_proxy.filename is identical
  #                on every machine (an absolute path would leak the applying
  #                machine's path into state and break cross-machine uploads). The
  #                build step wraps it in abspath() because build_lambda.sh runs with
  #                cwd = source_dir, where a relative target would land in the wrong
  #                place.
  lambdas = {
    alexa_proxy = {
      source_dir = abspath("${path.module}/../src/aws/lando-alexa-proxy")
      zip_path   = "${path.module}/.terraform/alexa_proxy.zip"
    }
  }

  tags = merge(var.tags, {
    Project   = "Lando",
    ManagedBy = "Terraform",
  })
}
