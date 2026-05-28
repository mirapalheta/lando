terraform {
  required_version = ">= 1.5" # 1.5+ for `import` blocks

  # NOTE: the backend "azurerm" block is intentionally NOT here.
  # It lives in backend.tf (gitignored), enabled by copying
  # backend.tf.example after the first apply has created the state container.
  # See the README's "State management" section for the bootstrap procedure.

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.74"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.8"
    }
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.94, < 7.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {
    application_insights {
      # Prevents Azure from auto-enabling its own unmanaged Failure Anomalies rule
      # Allows Terraform to manage the smart detector rule exclusively via IaC
      disable_generated_rule = true
    }
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "aws" {
  region = var.aws_region
}
