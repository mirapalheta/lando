# Deployment Guide

## Prerequisites

- Azure CLI installed and authenticated: `az login`
- AWS CLI installed and authenticated: `aws configure`
- GitHub CLI installed and authenticated: `gh auth login`
- Terraform installed (version >= 1.5)
- Tailscale auth key with `tag:lando` authorized
- Home Assistant long-lived access token
- Docker (for building the container image locally, if needed)

## Infrastructure Deployment

### 1. Configure Terraform Variables

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars: project name, location, HA token, certificate,
# Tailscale auth key, Alexa client IDs, skill ID, and image tag
```

### 2. Plan and Apply Infrastructure

```bash
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

This provisions all Azure and AWS resources in one pass, including generating
and storing the HMAC shared secret in both AWS Secrets Manager and Azure Key
Vault.

### 3. Wire up CI/CD (optional)

If you want GitHub Actions to build and deploy the container on every tagged
release, set the following secrets and variables on your fork so the workflow
can authenticate to Azure and AWS.

From the `terraform/` directory:

```bash
# Secrets
gh secret set AZURE_CLIENT_SECRET           --body "$(terraform output -raw github_actions_client_secret)"
gh secret set CONTAINER_REGISTRY_PASSWORD   --body "$(terraform output -raw container_registry_password)"
gh secret set AWS_ACCESS_KEY_ID             --body "$(terraform output -raw aws_github_actions_access_key_id)"
gh secret set AWS_SECRET_ACCESS_KEY         --body "$(terraform output -raw aws_github_actions_access_key_secret)"

# Variables
gh variable set AZURE_CLIENT_ID             --body "$(terraform output -raw github_actions_client_id)"
gh variable set AZURE_SUBSCRIPTION_ID       --body "$(terraform output -raw azure_subscription_id)"
gh variable set AZURE_TENANT_ID             --body "$(terraform output -raw azure_tenant_id)"
gh variable set CONTAINER_REGISTRY_URL      --body "$(terraform output -raw container_registry_url)"
gh variable set CONTAINER_REGISTRY_USERNAME --body "$(terraform output -raw container_registry_username)"
gh variable set AWS_REGION                  --body "$(terraform output -raw aws_region)"
```

All values come straight from Terraform outputs — no manual lookup needed.

If you prefer to deploy manually, skip this step and use `terraform apply` directly
whenever you want to update the running image (set `image_tag` in `terraform.tfvars`
and re-apply).

### 4. Verify container app settings

In Azure Portal → **Container App → Settings → Environment variables**, verify:

```
KEY_VAULT_URI                         = https://kv-<project>-<location>.vault.azure.net/
HomeAssistant__ClientOptions__BaseUrl = https://homeassistant.example.local:8123
```

Key Vault should contain the secrets Terraform created:

```
HomeAssistant-Token
HomeAssistant-Certificate      (if using a custom CA)
Hmac-SharedSecret
Alexa-SmartHome-Authorization-ClientSecret
Alexa-SmartHome-Event-ClientSecret
```

---

## Releasing a new version

Releases are triggered by a git tag. The GitHub Actions workflow builds and
pushes a Docker image to ACR, then deploys the new revision to the container app.

```bash
# Tag and push
git tag -a v0.2.0 -m "v0.2.0"
git push origin v0.2.0
```

GitHub Actions will: build → push image to ACR → update container app revision
→ create a GitHub Release.

**Rollback** to a previous image tag by updating `image_tag` in `terraform.tfvars`
and running `terraform apply`, or by re-pushing the old tag:

```bash
git tag -a v0.1.1 v0.1.0 -m "revert to v0.1.0 image"
git push origin v0.1.1
```

---

## Register Alexa Skill endpoint

After infrastructure is provisioned, point Alexa at the Lambda:

```bash
terraform output -raw alexa_smart_home_function_name
```

In the [Alexa Developer Console](https://developer.amazon.com/alexa/console/ask):
**Build → Smart Home → Default endpoint** → paste the Lambda function name.

Enable the skill in the Alexa app and run device discovery.

---

## Resource naming

All names are driven by `var.project_name` and `var.location`:

| Resource                  | Pattern                    | Example (`project=lando`, `location=eastus`) |
| ------------------------- | -------------------------- | -------------------------------------------- |
| Resource Group            | `rg-{project}-{location}`  | `rg-lando-eastus`                            |
| Storage Account           | `st{project}{location}`    | `stlandoeastus`                              |
| Key Vault                 | `kv-{project}-{location}`  | `kv-lando-eastus`                            |
| Container App Environment | `cae-{project}-{location}` | `cae-lando-eastus`                           |
| Container App             | `ca-{project}-{location}`  | `ca-lando-eastus`                            |
| Container Registry        | `acr{project}{location}`   | `acrlandoeastus`                             |

---

## Key Terraform outputs

```bash
terraform output                                      # all outputs
terraform output -raw container_app_url               # container app FQDN
terraform output -raw container_registry_url          # ACR login server
terraform output -raw key_vault_uri                   # Key Vault URI
terraform output -raw alexa_smart_home_function_name  # Lambda name for Alexa console
```

---

## Monitoring

Container app logs in Log Analytics:

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s startswith "ca-lando"
| order by TimeGenerated desc
| take 100
```

Lambda logs:

```bash
aws logs tail /aws/lambda/lambda-lando-alexa-proxy --follow
```

Azure Portal → **Cost Management → Cost Analysis** → filter by your resource
group to track spend.

---

## Troubleshooting

See [troubleshooting.md](troubleshooting.md) for symptom-first guidance on
401s, HA connectivity, ChangeReport silence, and Key Vault errors.
