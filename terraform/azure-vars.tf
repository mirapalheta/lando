# ========================================
# Azure-specific variables
# ========================================

variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
  sensitive   = false
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "centralus"
}

variable "image_tag" {
  description = "Docker image tag (defaults to latest git tag if not specified)"
  type        = string
}

# ----------------------------------------
# Home Assistant
# ----------------------------------------

variable "home_assistant_token" {
  description = "Home Assistant API token"
  type        = string
  sensitive   = true
}

variable "home_assistant_certificate" {
  description = "Home Assistant certificate (base64 encoded, no PEM headers)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "home_assistant_base_url" {
  description = "Home Assistant base URL (e.g., https://your-ha-domain:port)"
  type        = string
}

# ----------------------------------------
# Home Assistant trust-store certificates (BUILD-TIME)
# ----------------------------------------
# These point at PEM files on disk; their contents are read by
# scripts/build_image.sh and baked into the container image's OS trust
# store via the Dockerfile's ha_caf / ha_crt build secrets.
# Distinct from `home_assistant_certificate` above, which is the
# *runtime* certificate stored in Key Vault for the .NET HttpClient.
#
# Paths are resolved relative to the terraform working directory
# (lando/terraform/), so prefer absolute paths in terraform.tfvars unless
# you really mean a relative one.

variable "home_assistant_ca_file" {
  description = "Path to a PEM CA bundle file to install into the container's OS trust store at build time. Empty (default) = no CA installed."
  type        = string
  default     = ""
}

variable "home_assistant_cert_file" {
  description = "Path to a PEM cert file to install into the container's OS trust store at build time. Empty (default) = no cert installed."
  type        = string
  default     = ""
}

# ----------------------------------------
# Tailscale (Azure-side sidecar)
# ----------------------------------------

variable "tailscale_auth_key" {
  description = "Tailscale Auth Key for automatic device authentication (reusable, pre-auth)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "tailscale_accept_dns" {
  description = "Accept DNS configuration from Tailscale admin console (enables MagicDNS)"
  type        = bool
  default     = true
}

# ----------------------------------------
# Alexa OAuth (Login with Amazon) — used by Azure-side bearer-token validation
# ----------------------------------------

variable "alexa_smart_home_auth_client_id" {
  description = "Login-with-Amazon client_id of the Smart Home skill this bridge serves. Find it in the Alexa Developer Console under your skill's Permissions tab; format is amzn1.application-oa2-client.XXXXXXXX. Inbound bearer tokens whose 'aud' claim does not match this value are rejected."
  type        = string
}

variable "alexa_smart_home_auth_client_secret" {
  description = "Login-with-Amazon client secret paired with alexa_smart_home_auth_client_id. Used only for inbound token validation (no network calls required — the client_id is the pinned aud value). Stored as a Key Vault secret; never logged."
  type        = string
  sensitive   = true
  default     = ""
}

variable "alexa_smart_home_event_client_id" {
  description = "Login-with-Amazon client_id for the Alexa Event Gateway. Find it in the Alexa Developer Console under your skill's Permissions tab; format is amzn1.application-oa2-client.XXXXXXXX. Used during AcceptGrant code exchange and to refresh access tokens for proactive event delivery."
  type        = string
  default     = ""
}

variable "alexa_smart_home_event_client_secret" {
  description = "Login-with-Amazon client secret paired with alexa_smart_home_event_client_id. Required so the bridge can exchange the authorization code Alexa sends at AcceptGrant for a refresh+access token pair, and later refresh access tokens for the Alexa Event Gateway. Stored as a Key Vault secret; never logged."
  type        = string
  sensitive   = true
  default     = ""
}

# ----------------------------------------
# Container App runtime
# ----------------------------------------

variable "app_port" {
  description = "Port the application listens on"
  type        = number
  default     = 80
}

variable "socks5_port" {
  description = "Port the SOCKS5 proxy listens on"
  type        = number
  default     = 1055
}

variable "proxy_health_check_port" {
  description = "Port for health checks to verify proxy is running"
  type        = number
  default     = 9002
}

variable "hostname" {
  description = "Tailscale hostname. Use 'container_app' for full container app name, 'project_name' for project name, or provide a custom hostname"
  type        = string
  default     = "container_app"
}
