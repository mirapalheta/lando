# Azure — .NET Function App

[![.NET coverage](https://codecov.io/gh/mirapalheta/lando/graph/badge.svg?flag=dotnet)](https://app.codecov.io/gh/mirapalheta/lando?flags%5B0%5D=dotnet)

The Azure side of the Lando bridge. Receives HMAC-signed directives from the
AWS Lambda, verifies them, dispatches to the appropriate handler, calls Home
Assistant, and returns the Alexa response. Also runs a background service that
subscribes to HA state changes and sends proactive ChangeReports to the Alexa
Event Gateway.

---

## Project layout

```
src/azure/
├── Lando.Abstractions/               # Transport-agnostic interfaces and base types
│   ├── Handlers/IRequestHandler      # Generic request/response handler contract
│   ├── Validators/IRequestValidator  # Paired validator interface
│   ├── Security/ITokenStore          # Refresh-token persistence contract
│   └── Exceptions/LandoException     # Base exception with Alexa error type
│
├── Lando.Alexa.Core/                 # HMAC verification + LWA (Login-with-Amazon) token client
│   ├── Security/HMAC/                # HmacSignatureVerifier (constant-time, versioned)
│   └── Security/LWA/                 # LwaTokenClient — code exchange + token refresh
│
├── Lando.Alexa.SmartHome/            # Directive handlers, transformers, validators
│   ├── Handlers/Directives/          # One handler per directive name (Chain of Responsibility)
│   ├── Transformers/Entity/          # Per-domain discovery + state transformers (Strategy)
│   ├── Transformers/Payload/         # Per-directive HA service call translators
│   ├── Services/ChangeReportService  # Background WebSocket listener → Alexa Event Gateway
│   └── Validators/                   # FluentValidation validators per directive payload
│
├── Lando.HomeAssistant.Abstractions/ # HA-side interfaces and models
│   └── IHomeAssistantClient, IDeviceDiscovery, IServiceCaller, IHomeAssistantWebSocketClient
│
├── Lando.HomeAssistant.Core/         # REST + WebSocket implementations of the HA interfaces
│   ├── Services/HomeAssistantClient  # HTTP client with optional SOCKS5 proxy + custom CA
│   ├── Services/ServiceCallerService # Calls HA service endpoints
│   └── Services/DeviceDiscoveryService # Fetches and filters HA entity list
│
└── Lando.FunctionApp/                # Azure Functions host
    ├── Functions/Alexa/AlexaSmartHome.cs  # HTTP trigger — entry point for all directives
    ├── Functions/HealthCheck.cs            # /api/health liveness endpoint
    ├── Security/TokenStore.cs              # Key Vault-backed ITokenStore
    └── local.settings.example.json         # Local dev config template
```

Tests mirror this layout under `tests/`:

```
tests/
├── Lando.Abstractions.Tests/
├── Lando.Alexa.Core.Tests/
├── Lando.Alexa.SmartHome.Tests/
├── Lando.FunctionApp.Tests/
├── Lando.HomeAssistant.Abstractions.Tests/
└── Lando.HomeAssistant.Core.Tests/
```

---

## Build and test

```bash
# From the repo root
dotnet restore
dotnet build
dotnet test
```

Coverage report (outputs to `coverage/`):

```bash
./run-tests.sh
```

---

## Local development

The function app runs locally with Azure Functions Core Tools and Azurite for
local storage emulation.

**1. Copy and fill in local settings:**

```bash
cp src/azure/Lando.FunctionApp/local.settings.example.json \
   src/azure/Lando.FunctionApp/local.settings.json
# edit local.settings.json with your HA URL, token, Alexa client IDs, and HMAC secret
```

**2. Start Azurite (local Azure Storage):**

```bash
azurite
```

**3. Start the function:**

```bash
func start --project src/azure/Lando.FunctionApp
```

The smart-home endpoint is at `http://localhost:7071/api/alexa/smart-home`.

> **Note:** HMAC verification runs in production mode even locally. Raw `curl`
> requests without a valid `X-Lando-Signature` header will return 401 — that's
> expected. Use the `local-dev-secret-replace-me-for-prod-via-key-vault`
> placeholder from the example settings to sign test requests, or set
> `ASPNETCORE_ENVIRONMENT=Development` to relax signature checking.

---

## Deployment

Deployment is via Docker container pushed to Azure Container Registry, then
pulled by the Azure Container App. The GitHub Actions workflow in
`.github/workflows/build-test.yml` handles this on every tagged release.

See [docs/deployment.md](../../docs/deployment.md) for the full deploy flow
and [terraform/](../../terraform/README.md) for infrastructure provisioning.

---

## Architecture deep-dive

See [docs/architecture.md](../../docs/architecture.md) for:

- Inbound request pipeline (buffer → HMAC verify → deserialise → dispatch)
- Chain-of-Responsibility directive dispatch (`SmartHomeHandler` → `DirectiveHandler<,>`)
- Strategy pattern for entity transformation (`IEntityTransform<T>`)
- Proactive state — `ChangeReportService` diff and delivery
- Token storage — Key Vault-backed `ITokenStore`
- Transport security per hop
