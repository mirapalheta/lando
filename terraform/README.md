# Terraform — Lando Infrastructure

Provisions all Azure and AWS resources for the Lando bridge.

**Azure:** Container App (with Tailscale sidecar) + Container Registry +
Key Vault + Storage Account + Log Analytics + Application Insights +
User-Assigned Managed Identity.

**AWS:** Lambda proxy + IAM role/policy + Secrets Manager secret (HMAC key) +
CloudWatch log group + Alexa trigger permission.

---

## Prerequisites

- Terraform >= 1.0
- Azure CLI authenticated: `az login`
- AWS CLI authenticated: `aws configure` (or environment credentials)
- Home Assistant long-lived access token
- Tailscale auth key tagged `tag:lando`
- Alexa Smart Home skill IDs from the Alexa Developer Console

---

## Setup

### 1. Create terraform.tfvars

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your values. Required fields:

| Variable                               | Where to find it                                             |
| -------------------------------------- | ------------------------------------------------------------ |
| `subscription_id`                      | `az account show --query id -o tsv`                          |
| `home_assistant_token`                 | HA → Settings → Devices & Services → Long-Lived Access Token |
| `home_assistant_base_url`              | Your HA URL, e.g. `https://homeassistant.example.local:8123` |
| `alexa_smart_home_auth_client_id`      | Alexa Developer Console → your skill → Permissions tab       |
| `alexa_smart_home_auth_client_secret`  | Same location                                                |
| `alexa_smart_home_event_client_id`     | Same location (may be the same client)                       |
| `alexa_smart_home_event_client_secret` | Same location                                                |
| `alexa_smart_home_skill_id`            | Alexa Developer Console → your skill → Skill ID              |
| `tailscale_auth_key`                   | Tailscale admin console → Settings → Keys (tag: `tag:lando`) |
| `image_tag`                            | Docker image tag to deploy from ACR, e.g. `0.1.0`            |

Optional:

| Variable                     | Default     | Notes                                        |
| ---------------------------- | ----------- | -------------------------------------------- |
| `project_name`               | `lando`     | Drives all resource names                    |
| `location`                   | `centralus` | Azure region                                 |
| `aws_region`                 | `us-east-1` | AWS region for Lambda                        |
| `home_assistant_certificate` | _(empty)_   | Base64 CA cert if HA uses a self-signed cert |

### 2. Initialise Terraform

```bash
terraform init
```

### 3. Plan and apply

```bash
terraform plan -out=tfplan
terraform apply tfplan
```

This creates all Azure and AWS resources in one pass. Terraform also generates
and stores the HMAC shared secret in both AWS Secrets Manager and Azure Key
Vault so the Lambda and container app share the same key automatically.

---

## Resources created

**Azure**

| Resource                   | Name pattern                |
| -------------------------- | --------------------------- |
| Resource Group             | `rg-{project}-{location}`   |
| Storage Account            | `st{project}{location}`     |
| Key Vault                  | `kv-{project}-{location}`   |
| Container Registry (Basic) | `acr{project}{location}`    |
| Container App Environment  | `cae-{project}-{location}`  |
| Container App              | `ca-{project}-{location}`   |
| Log Analytics Workspace    | `log-{project}-{location}`  |
| Application Insights       | `appi-{project}-{location}` |
| User-Assigned Identity     | `id-{project}-{location}`   |

**AWS**

| Resource               | Name pattern                               |
| ---------------------- | ------------------------------------------ |
| Lambda function        | `lambda-{project}-alexa-proxy`             |
| IAM role               | `role-{project}-alexa-proxy`               |
| Secrets Manager secret | `{project}/hmac/shared-secret`             |
| CloudWatch log group   | `/aws/lambda/lambda-{project}-alexa-proxy` |

---

## GitHub Actions setup

After `terraform apply`, wire up GitHub Actions for container deployments:

```bash
chmod +x ../scripts/setup-github-secrets.sh
../scripts/setup-github-secrets.sh
```

The script reads from Terraform outputs and sets:

**Secrets**

| Name                          | Source                                               |
| ----------------------------- | ---------------------------------------------------- |
| `AZURE_CLIENT_SECRET`         | `terraform output -raw github_actions_client_secret` |
| `CONTAINER_REGISTRY_PASSWORD` | `terraform output -raw container_registry_password`  |

**Variables**

| Name                          | Source                                              |
| ----------------------------- | --------------------------------------------------- |
| `AZURE_CLIENT_ID`             | `terraform output -raw github_actions_client_id`    |
| `AZURE_SUBSCRIPTION_ID`       | `terraform output -raw azure_subscription_id`       |
| `AZURE_TENANT_ID`             | `terraform output -raw azure_tenant_id`             |
| `CONTAINER_REGISTRY_URL`      | `terraform output -raw container_registry_url`      |
| `CONTAINER_REGISTRY_USERNAME` | `terraform output -raw container_registry_username` |

---

## Register Alexa Skill endpoint

After `terraform apply`:

```bash
terraform output -raw alexa_smart_home_function_name
```

Paste that Lambda function name into the Alexa Developer Console under
**Build → Smart Home → Default endpoint**.

---

## Useful outputs

```bash
terraform output                                    # all outputs
terraform output -raw container_app_url             # container app FQDN
terraform output -raw container_registry_url        # ACR login server
terraform output -raw key_vault_uri                 # Key Vault URI
terraform output -raw alexa_smart_home_arn          # Lambda ARN
terraform output -raw alexa_smart_home_function_name  # Lambda name (for Alexa console)
```

---

## Cost estimate

Approximate monthly cost (as of 2026) for typical home use:

| Resource                     | Cost             |
| ---------------------------- | ---------------- |
| Container Apps (Consumption) | ~$1–3            |
| Container Registry (Basic)   | ~$5.00           |
| Key Vault                    | ~$0.60           |
| Storage Account              | ~$0.50           |
| Log Analytics                | ~$0–2            |
| AWS Lambda + Secrets Manager | <$1.00           |
| **Total**                    | **~$8–12/month** |

Container Apps Consumption pricing is per vCPU-second and GiB-second — actual
cost depends on how many Alexa commands you send per day.

---

## Troubleshooting

### Soft-deleted Key Vault

```bash
az keyvault list-deleted --query "[].name"
az keyvault purge --name <vault-name> --location <location>
terraform apply
```

### Container app not responding

```bash
# Check container app logs
az containerapp logs show \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --follow

# Or query Log Analytics
```

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "ca-lando-<location>"
| order by TimeGenerated desc
| take 100
```

### Lambda not forwarding requests

```bash
aws logs tail /aws/lambda/lambda-lando-alexa-proxy --follow
```

Check for `AZURE_ENDPOINT not set` or HTTP errors from the container app.

### Key Vault access denied

Verify the container app's managed identity has **Key Vault Secrets User** on
the vault:

```bash
az role assignment list \
  --assignee $(terraform output -raw container_app_principal_id) \
  --scope $(terraform output -raw key_vault_id)
```

### State management

Terraform state is stored locally. For shared or production use, migrate state
to Azure Storage:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-tfstate"
    storage_account_name = "sttfstate<unique>"
    container_name       = "tfstate"
    key                  = "lando.tfstate"
  }
}
```
