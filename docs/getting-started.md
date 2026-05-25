# Getting Started

Get Lando up and running in about 15 minutes.

## Prerequisites

Before starting, make sure you have:

- Azure subscription with Terraform infrastructure deployed (see [Deployment Guide](deployment.md))
- Tailscale network with `tag:lando` authorized and your HA host reachable
- Home Assistant long-lived access token stored in Key Vault
- This repository cloned locally

## 1. Clone & Build

```bash
git clone https://github.com/yourusername/lando.git
cd lando

# Restore dependencies
dotnet restore

# Build
dotnet build
```

## 2. Configure for Local Testing

Copy the settings template and fill in your values:

```bash
cp src/azure/Lando.FunctionApp/local.settings.example.json \
   src/azure/Lando.FunctionApp/local.settings.json
```

Edit `local.settings.json`:

```json
{
  "Values": {
    "HomeAssistant__BaseUrl": "https://homeassistant.example.local:443",
    "HomeAssistant__Token": "<your-ha-long-lived-token>",
    "Alexa__SmartHomeSkillId": "amzn1.ask.skill.XXXXX"
  }
}
```

## 3. Run Locally

```bash
# Terminal 1: local Azure Storage emulator
azurite

# Terminal 2: start the function
func start

# You should see:
# Azure Functions Core Tools
# Listening on http://0.0.0.0:7071
# AlexaSmartHome: [POST] http://localhost:7071/api/AlexaSmartHome
```

## 4. Test Discovery

```bash
curl -X POST http://localhost:7071/api/AlexaSmartHome \
  -H "Content-Type: application/json" \
  -d '{
    "directive": {
      "header": {
        "namespace": "Alexa.Discovery",
        "name": "Discover",
        "messageId": "test-123"
      },
      "payload": {}
    }
  }'
```

If you get a JSON response listing Home Assistant entities, the local setup is working. (Signature validation will reject the request in production — that's expected for a raw curl.)

## 5. Deploy to Azure

Lando runs as a Docker container on Azure Container Apps. Deployment is
triggered by a git tag — GitHub Actions builds the image, pushes it to ACR,
and updates the container app revision.

```bash
git add .
git commit -m "chore: initial config"
git tag v0.1.0
git push origin main --tags
# Watch: GitHub Actions > Build and Test → container app revision update
```

The container app URL is available from:

```bash
cd terraform && terraform output -raw container_app_url
```

See [docs/deployment.md](deployment.md) for the full release flow, rollback,
and GitHub Actions setup.

## 6. Register Alexa Skill

1. Go to [Alexa Developer Console](https://developer.amazon.com/alexa/console/ask)
2. Create a new skill — **Smart Home** type, **English (US)**
3. Go to **Build → Smart Home → Endpoint**
4. Paste your Function App URL:
   ```
   https://<your-function-app-name>.azurewebsites.net/api/AlexaSmartHome
   ```
5. Click **Save and Build**

## 7. Discover Devices

1. Open the Alexa app → **More → Skills & Games**
2. Search for and enable your skill
3. Go to **Devices → Discover Devices**

Your Home Assistant lights, switches, covers, and other supported entities will appear.

## 8. Test Voice Commands

```
"Alexa, turn on the living room lights"
"Alexa, set the bedroom lights to 50 percent"
"Alexa, turn off all lights"
```

Watch logs in real time:

```bash
# Azure Portal → your Function App → Log stream
# or Application Insights → Logs
```

---

## Troubleshooting

| Problem                           | Fix                                                                     |
| --------------------------------- | ----------------------------------------------------------------------- |
| Discovery returns empty           | Check HA token in local.settings.json; verify HA is reachable           |
| Function won't start              | Run `dotnet restore && dotnet build` first                              |
| 401 Unauthorized                  | Expected for raw curl tests; check Alexa signature setup for production |
| Alexa says "skill not responding" | Check Function App logs for errors; verify endpoint URL                 |
| Devices won't turn on             | Check HA token permissions; check HA logs                               |
| Can't reach Home Assistant        | Verify Tailscale connection (`tailscale status`) and ACL rules          |

## Verify Home Assistant Connectivity

```bash
# From a machine on your Tailscale network
curl -k https://<ha-host>:443/api/states \
  -H "Authorization: Bearer <your-ha-token>"
```

## Next Steps

- [Full Deployment Guide](deployment.md) — Terraform variables, CI/CD setup, monitoring
- [Architecture Overview](architecture.md) — How the pieces fit together
