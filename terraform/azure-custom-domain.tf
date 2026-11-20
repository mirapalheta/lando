# ========================================
# Custom domain (optional) — DNS records + managed-certificate bind
# ========================================
# Nothing here is created while var.custom_domain is null.
#
# Why a CLI step and not azurerm resources: azurerm_container_app_custom_domain
# and azurerm_container_app_environment_managed_certificate exist, but neither
# binds the managed certificate to the hostname, so the name would stay
# unbound (no TLS) and a CLI step would still be needed. The script runs
# `az containerapp hostname add` (Azure issues a managed certificate only for a
# hostname already on an app in the environment), then
# `az containerapp hostname bind`, which issues the free managed certificate
# (DigiCert, auto-renewed; about 5 minutes) and binds it with SNI.
#
# Later deploys keep the binding: azurerm_container_app's ingress.custom_domain
# is computed, so the provider re-sends what it read from Azure on every update.
# ========================================

locals {
  custom_domain = var.custom_domain

  # Record name inside the zone: "lando" for lando.example.com in example.com.
  custom_domain_record = (
    local.custom_domain == null ? null :
    local.custom_domain.azure_dns_zone == null ? null :
    trimsuffix(local.custom_domain.hostname, ".${local.custom_domain.azure_dns_zone.name}")
  )

  custom_domain_manage_dns = local.custom_domain != null && try(local.custom_domain.azure_dns_zone != null, false)
}

# The CNAME must point straight at the generated name, with nothing in between:
# that is what CNAME validation checks, and what managed-certificate renewal
# re-checks for the life of the certificate.
resource "azurerm_dns_cname_record" "custom_domain" {
  count               = local.custom_domain_manage_dns ? 1 : 0
  name                = local.custom_domain_record
  zone_name           = local.custom_domain.azure_dns_zone.name
  resource_group_name = local.custom_domain.azure_dns_zone.resource_group_name
  ttl                 = 300
  record              = azurerm_container_app.lando.ingress[0].fqdn
  tags                = local.tags
}

# Domain ownership proof. The ID is per subscription, not per app.
resource "azurerm_dns_txt_record" "custom_domain_asuid" {
  count               = local.custom_domain_manage_dns ? 1 : 0
  name                = "asuid.${local.custom_domain_record}"
  zone_name           = local.custom_domain.azure_dns_zone.name
  resource_group_name = local.custom_domain.azure_dns_zone.resource_group_name
  ttl                 = 3600
  tags                = local.tags

  record {
    value = azurerm_container_app.lando.custom_domain_verification_id
  }
}

# Hostname + managed certificate + SNI binding.
#   - Re-runs when the hostname changes, or when the app or its environment is
#     replaced (a new environment means a new generated name; Terraform rewrites
#     the CNAME first, then binds again).
#   - The script is idempotent: a hostname already bound with SniEnabled is left
#     alone, and an existing managed certificate for it is reused.
#   - On destroy (custom_domain set back to null, or a re-run above) the hostname
#     is removed from the app before the DNS records go. The managed certificate
#     stays in the environment, unused and free, until the environment is deleted.
resource "terraform_data" "custom_domain_bind" {
  count = local.custom_domain != null ? 1 : 0

  input = {
    hostname       = local.custom_domain.hostname
    container_app  = azurerm_container_app.lando.name
    resource_group = azurerm_resource_group.lando.name
    environment    = azurerm_container_app_environment.lando.name
  }

  lifecycle {
    replace_triggered_by = [
      azurerm_container_app.lando.id,
      azurerm_container_app_environment.lando.id,
    ]
  }

  provisioner "local-exec" {
    command     = "./scripts/bind_hostname.sh"
    working_dir = path.module
    environment = {
      CUSTOM_HOSTNAME = self.input.hostname
      CONTAINER_APP   = self.input.container_app
      RESOURCE_GROUP  = self.input.resource_group
      ENVIRONMENT     = self.input.environment
    }
  }

  provisioner "local-exec" {
    when       = destroy
    on_failure = continue
    command    = "az containerapp hostname delete --hostname \"$CUSTOM_HOSTNAME\" --name \"$CONTAINER_APP\" --resource-group \"$RESOURCE_GROUP\" --yes --output none"
    environment = {
      CUSTOM_HOSTNAME = self.input.hostname
      CONTAINER_APP   = self.input.container_app
      RESOURCE_GROUP  = self.input.resource_group
    }
  }

  depends_on = [
    azurerm_dns_cname_record.custom_domain,
    azurerm_dns_txt_record.custom_domain_asuid,
  ]
}
