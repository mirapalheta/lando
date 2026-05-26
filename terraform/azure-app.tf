# ========================================
# Container App + environment
# ========================================
# Two-container Pod: the .NET app container and a Tailscale sidecar that
# provides VPN connectivity to the home network.

# ----------------------------------------
# Container App Environment
# ----------------------------------------

resource "azurerm_container_app_environment" "lando" {
  name                       = local.names.container_app_environment
  location                   = azurerm_resource_group.lando.location
  resource_group_name        = azurerm_resource_group.lando.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.lando.id
  tags                       = var.tags

  depends_on = [azurerm_resource_group.lando]
}

# Register the storage account with the environment so the Tailscale sidecar
# can mount the file share defined in azure-storage.tf.
resource "azurerm_container_app_environment_storage" "lando" {
  name                         = local.names.storage_account
  container_app_environment_id = azurerm_container_app_environment.lando.id
  account_name                 = azurerm_storage_account.lando.name
  share_name                   = azurerm_storage_share.tailscale_state.name
  access_key                   = azurerm_storage_account.lando.primary_access_key
  access_mode                  = "ReadWrite"
}

# ----------------------------------------
# Container App with Tailscale sidecar + Function App
# ----------------------------------------

resource "azurerm_container_app" "lando" {
  name                         = local.names.container_app
  container_app_environment_id = azurerm_container_app_environment.lando.id
  resource_group_name          = azurerm_resource_group.lando.name
  revision_mode                = "Single"
  max_inactive_revisions       = 100
  # Merge the effective image tag in so the deployed version is visible
  # in the Azure Portal without having to inspect a revision's container
  # spec. Tracks terraform_data.image_state.output, so it updates only
  # when source files actually change (same trigger as the build itself).
  tags = merge(var.tags, {
    Version = terraform_data.image_state.output
  })

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.lando.id]
  }

  template {
    # ========================================
    # Volumes
    # ========================================
    # File Share for Tailscale state persistence
    # The gateway container mounts this to preserve its device identity and configuration
    # across Container App restarts and updates.
    volume {
      name         = "tailscale-state"
      storage_name = azurerm_storage_account.lando.name
      storage_type = "AzureFile"
    }

    # ========================================
    # Container Networking Model
    # ========================================
    # All containers in the same template block share a network namespace.
    # This means:
    # - They share the same IP address on the network interface
    # - They share the same routing table and network stack
    # - When the gateway container configures VPN routes, the app container automatically inherits them
    # - DNS resolution and all network configuration is shared across all containers
    # This is similar to how Kubernetes pods work with multiple containers.
    #
    # How app reaches HA through gateway:
    # 1. When app tries to reach HA, the DNS query goes through shared network stack
    # 2. The gateway container's tailscaled daemon has configured routes and DNS resolution
    # 3. The route is: app → shared network namespace → Tailscale tunnel → luke → home network → HA
    # ========================================

    # ========================================
    # Lando Function App container
    # ========================================
    # This container can reach Home Assistant because it inherits the VPN routes
    # and network configuration from the gateway container (shared network namespace).
    # Bare minimum allocation: Workload is I/O-bound (HTTP to Home Assistant), not CPU-intensive
    container {
      name = "app"
      # Use the effective tag persisted in terraform_data.image_state.output,
      # not var.image_tag directly. Bumping var.image_tag without changing
      # source code is a no-op; the container app keeps pointing at the
      # image that was actually built for the current sources.
      image  = "${azurerm_container_registry.lando.login_server}/${var.project_name}:${terraform_data.image_state.output}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = local.settings.app.ASPNETCORE_ENVIRONMENT
      }

      env {
        name  = "WEBSITES_PORT"
        value = local.settings.app.WEBSITES_PORT
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = local.settings.app.ASPNETCORE_URLS
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = local.secret_names_map.AppInsightsConnectionString
      }

      env {
        name        = "AzureWebJobsStorage"
        secret_name = local.secret_names_map.StorageConnectionString
      }

      env {
        name  = "KEY_VAULT_URI"
        value = azurerm_key_vault.lando.vault_uri
      }

      env {
        name  = "AZURE_CLIENT_ID"
        value = azurerm_user_assigned_identity.lando.client_id
      }

      env {
        name  = "HomeAssistant__ClientOptions__BaseUrl"
        value = local.settings.app.HomeAssistant__ClientOptions__BaseUrl
      }

      env {
        name  = "HomeAssistant__ClientOptions__ProxyAddress"
        value = local.settings.app.HomeAssistant__ClientOptions__ProxyAddress
      }

      env {
        name  = "HomeAssistant__ClientOptions__ProxyHealthCheckUrl"
        value = local.settings.app.HomeAssistant__ClientOptions__ProxyHealthCheckUrl
      }

      env {
        name        = "HomeAssistant__ClientOptions__Token"
        secret_name = local.secret_names_map.HomeAssistantToken
      }

      env {
        name  = "Alexa__SmartHome__Authorization__ClientId"
        value = local.settings.app.Alexa__SmartHome__Authorization__ClientId
      }

      env {
        name        = "Alexa__SmartHome__Authorization__ClientSecret"
        secret_name = local.secret_names_map.AlexaSmartHomeAuthClientSecret
      }

      env {
        name  = "Alexa__SmartHome__Event__ClientId"
        value = local.settings.app.Alexa__SmartHome__Event__ClientId
      }

      env {
        name        = "Alexa__SmartHome__Event__ClientSecret"
        secret_name = local.secret_names_map.AlexaSmartHomeEventClientSecret
      }

      # HMAC-SHA256 verification of inbound requests from the AWS Lambda proxy.
      # Same secret is replicated to AWS Secrets Manager (see aws-secrets.tf) so the
      # Lambda can sign with it.
      env {
        name        = "Hmac__SharedSecret"
        secret_name = local.secret_names_map.HmacSharedSecret
      }

      env {
        name  = "Hmac__MaxClockSkewSeconds"
        value = tostring(var.hmac_max_clock_skew_seconds)
      }

      # Liveness probe - HTTP check on port 80 to signal this container handles ingress
      liveness_probe {
        transport        = "HTTP"
        port             = local.settings.app.WEBSITES_PORT
        initial_delay    = 10
        interval_seconds = 10
      }
    }

    # ========================================
    # Tailscale gateway container - provides VPN connectivity to home network
    # ========================================
    # When tailscaled starts, it configures the network namespace with:
    # - Routes to Tailscale network (100.x.x.x range)
    # - Routes through the VPN gateway to the home network
    # Since containers share a network namespace, the app container automatically
    # has access to these routes and DNS resolution.
    # Bare minimum allocation: Tailscale daemon is very lean (~30MB actual usage)
    # Tailscale parameters:
    # https://tailscale.com/docs/features/containers/docker/docker-params
    container {
      name   = "gateway"
      image  = "tailscale/tailscale:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      # Override KUBERNETES_SERVICE_HOST to prevent containerboot from detecting Azure Container Apps
      # as Kubernetes and failing to initialize. Container Apps sets Kubernetes env vars but doesn't
      # provide the serviceaccount files containerboot expects, causing failures.
      # See: https://github.com/tailscale/tailscale/issues/18558
      env {
        name  = "KUBERNETES_SERVICE_HOST"
        value = ""
      }

      env {
        name  = "TS_STATE_DIR"
        value = "/var/lib/tailscale"
      }

      env {
        name        = "TS_AUTHKEY"
        secret_name = local.secret_names_map.TailscaleAuthKey
      }

      env {
        name  = "TS_HOSTNAME"
        value = local.settings.gateway.TS_HOSTNAME
      }

      env {
        name  = "TS_AUTH_ONCE"
        value = local.settings.gateway.TS_AUTH_ONCE
      }

      env {
        name  = "TS_ACCEPT_DNS"
        value = local.settings.gateway.TS_ACCEPT_DNS
      }

      env {
        name  = "TS_ENABLE_HEALTH_CHECK"
        value = local.settings.gateway.TS_ENABLE_HEALTH_CHECK
      }

      env {
        name  = "TS_LOCAL_ADDR_PORT"
        value = local.settings.gateway.TS_LOCAL_ADDR_PORT
      }

      env {
        name  = "TS_ENABLE_METRICS"
        value = local.settings.gateway.TS_ENABLE_METRICS
      }

      env {
        name  = "TS_ACCEPT_ROUTES"
        value = local.settings.gateway.TS_ACCEPT_ROUTES
      }

      env {
        name  = "TS_SOCKS5_SERVER"
        value = local.settings.gateway.TS_SOCKS5_SERVER
      }

      env {
        name  = "TS_USERSPACE"
        value = local.settings.gateway.TS_USERSPACE
      }

      # Liveness probe - HTTP check on health endpoint to restart container if Tailscale is unhealthy
      liveness_probe {
        transport        = "HTTP"
        path             = "/healthz"
        port             = var.proxy_health_check_port
        initial_delay    = 10
        interval_seconds = 30
      }

      # Mount the file share for Tailscale state persistence
      volume_mounts {
        name = "tailscale-state"
        path = "/var/lib/tailscale"
      }
    }

    min_replicas = 1
    max_replicas = 1
  }

  # Ingress configuration
  # Port mapping: target_port (80) is the port inside the app container.
  # Externally, Container Apps automatically provides HTTPS/TLS termination.
  # The public FQDN endpoint is always HTTPS (port 443) - Container Apps handles TLS cert generation.
  # Traffic flow: external HTTPS (443) → Container Apps TLS termination → app container port 80
  ingress {
    allow_insecure_connections = false # Require HTTPS externally
    external_enabled           = true  # Expose FQDN endpoint
    target_port                = 80    # Route to port 80 in app container
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # Registry configuration for ACR authentication
  # Using admin credentials since managed identity approach is unreliable with Container Apps
  registry {
    server               = azurerm_container_registry.lando.login_server
    username             = azurerm_container_registry.lando.admin_username
    password_secret_name = local.secret_names_map.AcrAdminPassword
  }

  secret {
    name  = local.secret_names_map.AcrAdminPassword
    value = azurerm_container_registry.lando.admin_password
  }

  secret {
    name  = local.secret_names_map.AppInsightsConnectionString
    value = azurerm_application_insights.lando.connection_string
  }

  # Key Vault secrets - dynamically created based on local.secrets.enabled
  dynamic "secret" {
    for_each = toset(local.secrets.enabled)
    content {
      name                = secret.value
      identity            = azurerm_user_assigned_identity.lando.id
      key_vault_secret_id = "${azurerm_key_vault.lando.vault_uri}secrets/${secret.value}"
    }
  }

  secret {
    name  = local.secret_names_map.StorageConnectionString
    value = azurerm_storage_account.lando.primary_connection_string
  }

  depends_on = [
    azurerm_container_app_environment.lando,
    azurerm_container_app_environment_storage.lando,
    azurerm_container_registry.lando,
    azurerm_key_vault.lando,
    azurerm_key_vault_secret.secrets,
    azurerm_user_assigned_identity.lando,
    azurerm_role_assignment.container_app_key_vault_secrets_user,
    azurerm_application_insights.lando,
    null_resource.acr_image_build
  ]
}
