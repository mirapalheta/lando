# Offline tests for var.custom_domain (azure-custom-domain.tf).
# Run from lando/terraform: terraform init -backend=false && terraform test
# Plan-only with mock providers: no cloud credentials, no provisioners run.

mock_provider "azurerm" {
  mock_data "azurerm_client_config" {
    defaults = {
      tenant_id       = "11111111-1111-1111-1111-111111111111"
      object_id       = "22222222-2222-2222-2222-222222222222"
      client_id       = "33333333-3333-3333-3333-333333333333"
      subscription_id = "00000000-0000-0000-0000-000000000000"
    }
  }

  override_resource {
    target          = azurerm_container_app.lando
    override_during = plan
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-lando-centralus/providers/Microsoft.App/containerApps/ca-lando-centralus"
      ingress = {
        fqdn = "ca-lando-centralus.example.centralus.azurecontainerapps.io"
      }
      custom_domain_verification_id = "0000000000000000000000000000000000000000000000000000000000000000"
    }
  }
}
mock_provider "azuread" {}
mock_provider "aws" {
  mock_data "aws_caller_identity" {
    defaults = {
      account_id = "123456789012"
      arn        = "arn:aws:iam::123456789012:user/test"
    }
  }
}
mock_provider "random" {}
mock_provider "null" {}

variables {
  subscription_id                 = "00000000-0000-0000-0000-000000000000"
  app_version                     = "0.0.0-test"
  home_assistant_token            = "test"
  home_assistant_base_url         = "https://ha.example.test:8123"
  alexa_smart_home_auth_client_id = "amzn1.application-oa2-client.test"
  alexa_skill_id                  = "amzn1.ask.skill.test"
}

run "no_custom_domain" {
  command = plan

  assert {
    condition     = length(azurerm_dns_cname_record.custom_domain) == 0 && length(azurerm_dns_txt_record.custom_domain_asuid) == 0 && length(terraform_data.custom_domain_bind) == 0
    error_message = "custom_domain = null must create nothing"
  }

  assert {
    condition     = aws_lambda_function.alexa_proxy.environment[0].variables["AZURE_ENDPOINT"] == "https://ca-lando-centralus.example.centralus.azurecontainerapps.io/api/alexa"
    error_message = "Lambda must call the generated name"
  }

  assert {
    condition     = output.custom_domain_hostname == null
    error_message = "custom_domain_hostname must be null"
  }
}

run "bind_with_azure_dns_lambda_unchanged" {
  command = plan

  variables {
    custom_domain = {
      hostname       = "lando.example.com"
      azure_dns_zone = { name = "example.com", resource_group_name = "rg-dns" }
    }
  }

  assert {
    condition     = azurerm_dns_cname_record.custom_domain[0].name == "lando" && azurerm_dns_cname_record.custom_domain[0].record == "ca-lando-centralus.example.centralus.azurecontainerapps.io" && azurerm_dns_cname_record.custom_domain[0].ttl == 300
    error_message = "CNAME lando → generated name, TTL 300"
  }

  assert {
    condition     = azurerm_dns_txt_record.custom_domain_asuid[0].name == "asuid.lando" && azurerm_dns_txt_record.custom_domain_asuid[0].zone_name == "example.com" && azurerm_dns_txt_record.custom_domain_asuid[0].resource_group_name == "rg-dns"
    error_message = "asuid.lando TXT in the given zone"
  }

  assert {
    condition     = terraform_data.custom_domain_bind[0].input.hostname == "lando.example.com" && terraform_data.custom_domain_bind[0].input.container_app == "ca-lando-centralus"
    error_message = "bind step targets the hostname and the app"
  }

  assert {
    condition     = aws_lambda_function.alexa_proxy.environment[0].variables["AZURE_ENDPOINT"] == "https://ca-lando-centralus.example.centralus.azurecontainerapps.io/api/alexa"
    error_message = "lambda_endpoint defaults to false: the Lambda must still call the generated name"
  }
}

run "manual_dns_binds_without_records" {
  command = plan

  variables {
    custom_domain = { hostname = "lando.example.org" }
  }

  assert {
    condition     = length(azurerm_dns_cname_record.custom_domain) == 0 && length(azurerm_dns_txt_record.custom_domain_asuid) == 0 && length(terraform_data.custom_domain_bind) == 1
    error_message = "without azure_dns_zone: no records, bind step only"
  }
}

# In real use the bind step already exists (step 1), so its output is known
# from state when lambda_endpoint flips; the override stands in for that state.
run "lambda_switches_to_custom_name" {
  command = plan

  override_resource {
    target          = terraform_data.custom_domain_bind[0]
    override_during = plan
    values = {
      output = {
        hostname       = "lando.example.com"
        container_app  = "ca-lando-centralus"
        resource_group = "rg-lando-centralus"
        environment    = "cae-lando-centralus"
      }
    }
  }

  variables {
    custom_domain = {
      hostname        = "lando.example.com"
      azure_dns_zone  = { name = "example.com", resource_group_name = "rg-dns" }
      lambda_endpoint = true
    }
  }

  assert {
    condition     = aws_lambda_function.alexa_proxy.environment[0].variables["AZURE_ENDPOINT"] == "https://lando.example.com/api/alexa"
    error_message = "lambda_endpoint = true must point the Lambda at the custom hostname"
  }

  assert {
    condition     = output.alexa_endpoint == "https://lando.example.com/api/alexa"
    error_message = "alexa_endpoint output must follow the Lambda"
  }
}

run "rejects_apex_hostname" {
  command = plan

  variables {
    custom_domain = {
      hostname       = "example.com"
      azure_dns_zone = { name = "example.com", resource_group_name = "rg-dns" }
    }
  }

  expect_failures = [var.custom_domain]
}

run "rejects_hostname_outside_zone" {
  command = plan

  variables {
    custom_domain = {
      hostname       = "lando.example.net"
      azure_dns_zone = { name = "example.com", resource_group_name = "rg-dns" }
    }
  }

  expect_failures = [var.custom_domain]
}

run "rejects_upper_case_hostname" {
  command = plan

  variables {
    custom_domain = { hostname = "Lando.Example.com" }
  }

  expect_failures = [var.custom_domain]
}
