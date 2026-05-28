# Terraform — Lando Infrastructure

Provisions all Azure and AWS resources for the Lando bridge.

**Azure:** Container App (with Tailscale sidecar) + Container Registry +
Key Vault + Storage Account + Log Analytics + Application Insights +
User-Assigned Managed Identity.

**AWS:** Lambda proxy + IAM role/policy + Secrets Manager secret (HMAC key) +
CloudWatch log group + Alexa trigger permission.

---

## Prerequisites

- Terraform >= 1.5 (uses `replace_triggered_by`)
- Docker daemon running locally — `terraform apply` builds the Azure
  Function App image with `docker build` (BuildKit) before pushing to ACR
- Node.js 24+ and npm — `terraform apply` runs `npm ci && npm run build`
  for the AWS Lambda bundle
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
| `app_version`                          | Docker image tag to deploy from ACR, e.g. `0.1.0`            |

Optional:

| Variable                     | Default     | Notes                                                                                                                                                |
| ---------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `project_name`               | `lando`     | Drives all resource names                                                                                                                            |
| `location`                   | `centralus` | Azure region                                                                                                                                         |
| `aws_region`                 | `us-east-1` | AWS region for Lambda                                                                                                                                |
| `home_assistant_certificate` | _(empty)_   | Base64 CA cert handed to the .NET HttpClient at **runtime** (stored in Key Vault)                                                                    |
| `home_assistant_ca_file`     | _(empty)_   | Path to a PEM CA bundle baked into the container's OS trust store at **build time**. Path is relative to `lando/terraform/`. Empty = no CA installed |
| `home_assistant_cert_file`   | _(empty)_   | Path to a PEM cert with the same build-time semantics as `home_assistant_ca_file`                                                                    |
| `tailscale_version`          | `latest`    | Tailscale gateway sidecar image tag (e.g. `v1.98.3`). `latest` always resolves to the most recent stable release.                                    |

### 2. Initialise Terraform (first run uses local state)

```bash
terraform init
```

> On a fresh checkout there is no `backend.tf` file — only
> `backend.tf.example` — so `terraform init` cleanly defaults to the local
> backend. The first `terraform apply` will create the storage account and
> the `tfstate` container as a side effect. After that, follow
> **[State management](#state-management)** to enable the remote backend
> and migrate the local state into Azure Blob Storage.

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
| Log Analytics Workspace    | `law-{project}-{location}`  |
| Application Insights       | `appi-{project}-{location}` |
| User-Assigned Identity     | `uai-{project}-{location}`  |

**AWS**

| Resource               | Name pattern                                    |
| ---------------------- | ----------------------------------------------- |
| Lambda function        | `lambda-{project}-alexa-smart-home`             |
| IAM role               | `role-{project}-alexa-smart-home`               |
| Secrets Manager secret | `{project}/hmac/shared-secret`                  |
| CloudWatch log group   | `/aws/lambda/lambda-{project}-alexa-smart-home` |

---

## How the image and bundle are built

Terraform drives both builds during `apply` — there is no separate CI pipeline
in the loop. The previous GitHub Actions workflow is archived under
`.scrub/deploy.yml` for reference.

**Azure Function App image** (`azure-app-build.tf` → `scripts/build_image.sh`):

1. `terraform_data.image_tag` holds the **effective image tag** in
   state. It's replaced only when the source-files hash changes, so
   `var.app_version` bumps without a code change are no-ops:
   `terraform_data.image_tag.output` keeps returning the tag captured
   the last time sources actually changed. The container app reads from
   `terraform_data.image_tag.output`, never `var.app_version` directly.
2. `null_resource.acr_image_build` is keyed on
   `terraform_data.image_tag.id`, so the build script only runs when
   sources changed (which is also when a fresh `var.app_version` is
   captured).
3. When the script runs, it queries ACR (`az acr repository show-tags`)
   and aborts if the tag already exists — bump `image_tag` or delete
   the existing tag before re-running.
4. Otherwise `docker build` runs locally with `--platform linux/amd64`,
   passing `COMMIT` / `BRANCH` / `VERSION` as build args and mounting
   `home_assistant_ca_file` / `home_assistant_cert_file` as BuildKit
   secrets (`ha_caf` / `ha_crt`), then `docker push` ships it to ACR.

**AWS Lambda bundle** (`aws-lambda-build.tf` → `scripts/build_lambda.sh`):

1. `local.alexa_smart_home_code_hash` is a base64-sha256 over the `.ts`
   / `package.json` / `package-lock.json` / `tsconfig.json` files. It
   drives both `null_resource.lambda_build`'s trigger and
   `aws_lambda_function.alexa_smart_home.source_code_hash`, so the
   Lambda update decision is computable from sources alone — no
   dependency on `dist/` or the zip existing at plan time.
2. When the hash changes, `scripts/build_lambda.sh` runs `npm ci && npm
run build` and then zips `dist/` straight into the path passed via
   `$ZIP_PATH` (no separate `archive_file` resource — the trigger is the
   freshness check). The script is unconditional: if it's running,
   sources differ from what produced the last artifacts.
3. `aws_lambda_function.alexa_smart_home` references the zip path via
   `filename`. The AWS provider only reads the file when
   `source_code_hash` differs from state, so on a fresh CI runner with
   no `dist/` and no zip, plan + apply still succeed when sources
   haven't changed.

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

## State management

Terraform state lives in an Azure Blob Storage container (`tfstate`) inside
the same storage account the stack creates (`stlando<location>`). The backend
authenticates via Entra ID, and the `Storage Blob Data Contributor` role on
the state container is granted to the Terraform-running identity by the stack
itself (`azurerm_role_assignment.tfstate_admin` in `azure-storage.tf`). The
storage account is hardened with TLS 1.2, blob versioning, and 30-day soft
delete so state is recoverable from accidental corruption or deletion.

When CI is also wired to run `terraform apply` (see the GitHub Actions
deploy workflow), the CI service principal gets its own
sibling grant — `azurerm_role_assignment.github_actions_tfstate_data`,
also in `azure-storage.tf`. The human's `tfstate_admin` carries a
`lifecycle { ignore_changes = [principal_id] }` block so apply never
re-points the human's role assignment to whichever identity is currently
running — both identities end up with permanent, independent grants.
The same pattern applies to `terraform_keyvault_admin` /
`github_actions_keyvault_secrets` in `azure-keyvault.tf` for Key Vault
data-plane access.

The state container is created by the stack itself, so the first run of a
fresh checkout uses local state, then migrates into Azure once the container
exists.

> [!NOTE]
> Because Terraform creates a role assignment as part of the apply, the
> identity running `terraform apply` needs `User Access Administrator` or
> `Owner` on the subscription — `Contributor` alone is not enough. Personal
> Azure accounts have this by default; on a corporate subscription you may
> need to ask the directory admin.

### Bootstrap from a fresh checkout

```bash
# 1. First apply with local state.
#    No backend.tf exists yet, so terraform init uses the local backend.
#    The apply creates the storage account and tfstate container.
terraform init
terraform apply

# 2. Enable the remote backend by copying the example file into place.
#    (The role assignment granting you Storage Blob Data Contributor on the
#    state container was already created by step 1 — no extra grant needed.)
cp backend.tf.example backend.tf
cp backend.hcl.example backend.hcl
# Edit backend.hcl with your resource_group_name and storage_account_name
# (defaults are correct for a centralus deployment with project_name = lando).

# 3. Re-init against the remote backend and migrate the local state up.
terraform init -backend-config=backend.hcl -migrate-state
# Answer "yes" when prompted to copy existing state to the new backend.

# 4. The local terraform.tfstate file is now safe to delete.
rm terraform.tfstate terraform.tfstate.backup
```

### Subsequent runs

```bash
terraform init -backend-config=backend.hcl   # only needed when .terraform/ is absent
terraform plan -out=tfplan
terraform apply tfplan
```

### Adding a collaborator

Two paths, pick one:

**Quick path** — grant ad-hoc via `az`, no Terraform changes needed:

```bash
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee "user@example.com" \
  --scope "$(terraform output -raw storage_account_id)/blobServices/default/containers/tfstate"
```

**Tracked path** — add a `state_admin_object_ids` variable to your stack
and grant via Terraform so the team-with-access is visible in code. The
hook to extend `azurerm_role_assignment.tfstate_admin` into a `for_each`
over a list of principal IDs is straightforward — open an issue or PR if
you want this wired up by default.

In both cases the collaborator then `az login`s, copies
`backend.tf.example` → `backend.tf` and `backend.hcl.example` →
`backend.hcl`, and runs `terraform init -backend-config=backend.hcl`.

### Recovering a deleted or corrupted state blob

Blob versioning and soft delete are enabled with a 30-day retention window.
The simplest path is the Azure Portal: navigate to the storage account →
**Data storage** → **Containers** → **tfstate** → select `lando.tfstate` →
**Versions** tab → restore the version you want.

From the CLI:

```bash
# List versioned + soft-deleted blobs
az storage blob list \
  --account-name <storage-account-name> \
  --container-name tfstate \
  --include "dv" \
  --auth-mode login \
  --query "[].{name:name, versionId:versionId, isDeleted:deleted}" -o table

# Promote an older version to be the current blob
az storage blob copy start \
  --account-name <storage-account-name> \
  --destination-container tfstate \
  --destination-blob lando.tfstate \
  --source-blob lando.tfstate \
  --source-container tfstate \
  --version-id <version-id> \
  --auth-mode login
```

### State locking

The `azurerm` backend takes a blob lease on the state file for the duration of
each `terraform plan` / `apply`, so concurrent runs refuse to race. If a run
crashes and leaves a stale lock:

```bash
terraform force-unlock <lock-id>
```

Only run this after confirming no one else is mid-apply — the lock ID is
printed in the error message of any subsequent run that hits the stale lease.

> [!CAUTION]
> The storage account that holds Terraform state is the **same** one the rest
> of the stack uses. `lifecycle { prevent_destroy = true }` blocks Terraform
> from destroying it, but `az group delete --name rg-lando-<location>` would
> still take state with it. If you ever need to tear the project down, copy
> the state blob out first or accept that you're rebuilding from scratch.

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
