# Configuration Reference

All runtime configuration is injected as environment variables. In production,
secrets are stored in Azure Key Vault and surfaced to the container app via
`@Microsoft.KeyVault(VaultName=…;SecretName=…)` app-setting references. In
local development they live in `local.settings.json` (gitignored).

---

## Azure container app settings

Copy `src/azure/Lando.FunctionApp/local.settings.example.json` to
`local.settings.json` and fill in your values for local development.

### Required

| Setting                                         | Description                                                                                                                                                                                  |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `KEY_VAULT_URI`                                 | URI of the Azure Key Vault that holds all secrets, e.g. `https://kv-lando-eastus.vault.azure.net/`. The container app's managed identity must hold **Key Vault Secrets User** on this vault. |
| `HomeAssistant__ClientOptions__BaseUrl`         | Full HTTPS URL of your Home Assistant instance, e.g. `https://homeassistant.example.local:8123`                                                                                              |
| `HomeAssistant__ClientOptions__Token`           | Long-lived access token from **Settings → Devices & Services → Create Long-Lived Access Token**                                                                                              |
| `Alexa__SmartHome__Authorization__ClientId`     | LWA client ID from the Alexa Developer Console (used in the Accept Grant OAuth flow)                                                                                                         |
| `Alexa__SmartHome__Authorization__ClientSecret` | Paired secret for the LWA client above                                                                                                                                                       |
| `Alexa__SmartHome__Event__ClientId`             | LWA client ID used to obtain tokens for the Alexa Event Gateway (proactive ChangeReports)                                                                                                    |
| `Alexa__SmartHome__Event__ClientSecret`         | Paired secret for the event LWA client                                                                                                                                                       |
| `Hmac__SharedSecret`                            | Shared HMAC secret — must match the value in AWS Secrets Manager exactly                                                                                                                     |

### Optional

| Setting                                             | Default   | Description                                                                                               |
| --------------------------------------------------- | --------- | --------------------------------------------------------------------------------------------------------- |
| `HomeAssistant__ClientOptions__Certificate`         | _(empty)_ | Base64-encoded CA certificate for self-signed HA HTTPS certs. No PEM headers.                             |
| `HomeAssistant__ClientOptions__ProxyAddress`        | _(empty)_ | SOCKS5 proxy address for routing HA traffic through the Tailscale sidecar, e.g. `socks5://127.0.0.1:1055` |
| `HomeAssistant__ClientOptions__ProxyHealthCheckUrl` | _(empty)_ | URL polled to verify the proxy is reachable before the app attempts HA calls                              |
| `Hmac__MaxClockSkewSeconds`                         | `300`     | Maximum tolerated difference (seconds) between the `X-Lando-Timestamp` header and the server clock        |

---

## Azure Key Vault secrets

The container app reads the following secrets by name. The managed identity
must hold the **Key Vault Secrets User** role on the vault.

| Secret name                                  | Corresponding app setting                       |
| -------------------------------------------- | ----------------------------------------------- |
| `HomeAssistant-Token`                        | `HomeAssistant__ClientOptions__Token`           |
| `HomeAssistant-Certificate`                  | `HomeAssistant__ClientOptions__Certificate`     |
| `Alexa-SmartHome-Authorization-ClientSecret` | `Alexa__SmartHome__Authorization__ClientSecret` |
| `Alexa-SmartHome-Event-ClientSecret`         | `Alexa__SmartHome__Event__ClientSecret`         |
| `Hmac-SharedSecret`                          | `Hmac__SharedSecret`                            |

Secrets are referenced in the Terraform-generated container app settings as:

```
@Microsoft.KeyVault(VaultName=<vault>;SecretName=<secret>)
```

---

## AWS Lambda environment variables

Set in `terraform/terraform.tfvars` (or as Terraform variables); Terraform
writes them to the Lambda function configuration.

| Variable             | Required            | Description                                                                                                                                                                                    |
| -------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AZURE_ENDPOINT`     | Yes                 | Base URL of the Azure container app's Alexa endpoint, e.g. `https://ca-<project>-<location>.azurecontainerapps.io/api/alexa`. The Lambda appends `/smart-home` or `/custom-skill` per payload. |
| `HMAC_SECRET_ARN`    | Yes                 | ARN of the AWS Secrets Manager secret holding the HMAC shared key                                                                                                                              |
| `FORWARD_TIMEOUT_MS` | No (default `8000`) | Outbound HTTP timeout in milliseconds. Keep below Alexa's 8-second response budget.                                                                                                            |

---

## Terraform variables

Full list in `terraform/terraform.tfvars.example`. The most commonly adjusted:

| Variable                     | Default     | Description                                                                           |
| ---------------------------- | ----------- | ------------------------------------------------------------------------------------- |
| `subscription_id`            | —           | Azure subscription ID                                                                 |
| `project_name`               | `lando`     | Drives all resource names: `rg-{project}-{location}`, `kv-{project}-{location}`, etc. |
| `location`                   | `eastus`    | Azure region                                                                          |
| `home_assistant_token`       | —           | Stored in Key Vault at apply time                                                     |
| `home_assistant_certificate` | —           | Base64 CA cert, no PEM headers (optional)                                             |
| `home_assistant_base_url`    | —           | Written to container app settings                                                     |
| `alexa_skill_id`             | —           | Locks the Lambda permission to your Alexa skill — one skill, both Smart Home and Custom models |
| `aws_region`                 | `us-east-1` | AWS region for the Lambda                                                             |

---

## Tailscale ACL

The container app's Tailscale sidecar needs to reach your Home Assistant
instance. Add this entry in the Tailscale admin console:

```json
{
  "action": "accept",
  "src": ["tag:lando"],
  "dst": ["<ha-tailscale-ip>:8123"]
}
```

Replace `<ha-tailscale-ip>` with the Tailscale IP of your Home Assistant host
(visible in the Tailscale admin console under **Machines**). If your HA instance
uses a port other than `8123`, adjust accordingly.

Tag the Tailscale auth key with `tag:lando` when you create it so the ACL rule
applies to the sidecar automatically.

> **Tip:** Use a subnet router (OPNsense, a Raspberry Pi, or a Tailscale-enabled
> VM on your LAN) so you don't need to install Tailscale directly on your HA
> host. The subnet router needs `tag:lando` reachability to the HA host on your
> local network instead.

---

## HMAC shared secret

The HMAC secret ties the AWS Lambda and the Azure container app together. It
must be identical in both places:

- **AWS side:** stored in Secrets Manager under the ARN referenced by
  `HMAC_SECRET_ARN`. Managed by Terraform in `terraform/aws-secrets.tf`.
- **Azure side:** stored in Key Vault under `Hmac-SharedSecret`, referenced
  by the `Hmac__SharedSecret` app setting.

Terraform generates a random secret and writes it to both at `apply` time. If
you rotate it manually, update both sides simultaneously — any in-flight
requests signed with the old secret will be rejected during the overlap window.

Generate a suitable secret with:

```bash
openssl rand -hex 32
```
