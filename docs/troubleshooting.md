# Troubleshooting

A symptom-first guide for the most common failure modes. Start with the container app logs (Log Analytics or Azure Portal → your
Container App → **Log stream**) and the AWS Lambda CloudWatch log group
(`/aws/lambda/lambda-<project>-alexa-proxy`).

---

## Alexa says "that device isn't responding"

This is the Alexa catch-all for any non-200 response or timeout from the
Lambda or the Azure Function.

**Check the Lambda first:**

```bash
# Tail Lambda logs (requires AWS CLI)
aws logs tail /aws/lambda/lambda-lando-alexa-proxy --follow
```

Look for `AZURE_ENDPOINT not set`, HTTP 4xx/5xx from the Azure Function, or
timeout errors.

**Check the container app:**

Log Analytics query for recent errors:

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s startswith "ca-lando"
| where Log_s contains "ERROR" or Log_s contains "Exception"
| order by TimeGenerated desc
| take 50
```

Or stream logs directly:

```bash
az containerapp logs show \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --follow
```

---

## 401 Unauthorized from the Azure Function

The function returned 401, which means HMAC verification failed.

1. **Clock skew** — the Lambda's `X-Lando-Timestamp` is more than
   `Hmac__MaxClockSkewSeconds` (default 300 s) from the function's clock.
   Lambda and Azure are both cloud-synced, so this is rare but possible
   after a Lambda cold start. Check the timestamps in the logs.

2. **Mismatched secret** — the HMAC secret in AWS Secrets Manager does not
   match the one in Azure Key Vault. Rotate both at the same time. See
   [configuration.md](configuration.md#hmac-shared-secret).

3. **Truncated body** — the Lambda is forwarding a body that differs from
   what was signed. Check that no API Gateway or proxy layer is modifying
   the payload between Alexa and the Lambda.

---

## Azure Function can't reach Home Assistant

Symptoms: `HttpRequestException`, `SocketException`, or HA returning 401 in
the function logs.

1. **Tailscale connectivity** — from any machine on your Tailscale network:

   ```bash
   curl -k https://<ha-tailscale-ip>:8123/api/ \
     -H "Authorization: Bearer <token>"
   ```

   If this fails, the issue is Tailscale, not Lando.

2. **ACL rules** — verify the `tag:lando` → `<ha-host>:<port>` ACL is present
   in the Tailscale admin console. See [configuration.md](configuration.md#tailscale-acl).

3. **Proxy address** — if you're routing through a subnet router, confirm
   `HomeAssistant__ClientOptions__ProxyAddress` points to the correct SOCKS5
   address and the proxy health check URL is responding.

4. **Certificate** — if your HA instance uses a self-signed cert, confirm
   `HomeAssistant__ClientOptions__Certificate` (or the Key Vault secret
   `HomeAssistant-Certificate`) holds the base64-encoded CA cert with no
   PEM headers, no newlines.

5. **Token** — regenerate the long-lived access token in HA (**Settings →
   Devices & Services → Long-lived Access Token**) and update the Key Vault
   secret, then restart the container app revision.

---

## Devices not appearing after "Discover devices"

1. **Empty discovery response** — check the function logs for a successful
   `/api/states` call to HA. If the call succeeds but the response is empty,
   the entities may all be in unsupported domains or have no `friendly_name`.

2. **Unsupported domain** — only `light`, `switch`, `cover`, `fan`, `lock`,
   `climate`, `media_player`, and `sensor` (numeric temperature) have
   registered transformers. Other domains are silently skipped. See
   [extending-device-types.md](extending-device-types.md) to add a new domain.

3. **AcceptGrant not completed** — discovery returns an empty list if Lando
   has never received a successful `Alexa.Authorization.AcceptGrant` directive.
   Disable and re-enable the skill in the Alexa app to trigger the grant flow,
   then check the function logs for `AcceptGrant` and a 200 from the LWA token
   endpoint.

4. **Alexa skill endpoint URL wrong** — verify the Lambda function name in the Alexa Developer Console matches
   `terraform output -raw alexa_proxy_function_name`.

---

## ChangeReports not delivered (Alexa app state is stale)

Lando sends proactive `ChangeReport` events via the Alexa Event Gateway
whenever HA fires a `state_changed` event on a discovered entity.

1. **WebSocket not connected** — check the function logs for
   `HomeAssistantWebSocketClient` reconnect messages. The client uses
   exponential backoff; a permanent failure here means HA is unreachable.

2. **No LWA grant** — ChangeReports require a valid per-grantee access
   token from the Alexa Event Gateway. If `AcceptGrant` was never completed
   (or the Key Vault secret was deleted), `ChangeReportService` logs a
   `LwaTokenException`. Re-enable the skill to trigger a fresh grant.

3. **Event Gateway rejects the report** — the gateway returns 202 for both
   success and most errors (it's async). Look for `400` responses in the
   function logs, which indicate a malformed payload. Capture the raw
   `ChangeReport` JSON from the logs and validate it against the
   [Alexa ChangeReport spec](https://developer.amazon.com/en-US/docs/alexa/smarthome/state-reporting-for-a-smart-home-skill.html).

4. **Empty delta** — HA fires `state_changed` on timestamp bumps that don't
   move any Alexa-visible property. Lando skips these intentionally; silence
   in the logs after a HA state change is normal for entities whose Alexa
   properties didn't actually move.

---

## Key Vault access errors on startup

Symptom: container app starts but immediately logs `SecretClientException` or
`CredentialUnavailableException`.

1. Confirm `KEY_VAULT_URI` is set correctly in the container app environment
   variables.
2. Verify the container app's managed identity is assigned the
   **Key Vault Secrets User** role on the vault:
   ```bash
   az role assignment list \
     --assignee <principal-id> \
     --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<kv>
   ```
   Or via Terraform: `terraform output -raw container_app_principal_id`.
3. Check that the secret names in Key Vault match exactly what the app
   settings reference (case-sensitive).

---

## Terraform apply fails with "soft-deleted Key Vault"

```bash
az keyvault list-deleted --query "[].name"
az keyvault purge --name <vault-name> --location <location>
terraform apply
```

---

## Container app not starting after deploy

Check the container app logs for startup errors:

```bash
az containerapp logs show \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --follow
```

Common causes: `KEY_VAULT_URI` not set, Key Vault RBAC not propagated yet
(wait a few minutes after `terraform apply`), or the ACR image tag referenced
in `terraform.tfvars` doesn't exist yet (push a build first).
