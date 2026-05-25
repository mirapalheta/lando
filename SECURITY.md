# Security Policy

## Supported versions

| Version | Supported |
| ------- | --------- |
| latest  | ✅        |

Lando is a self-hosted project. There is no commercial support tier. Security
fixes are released as soon as they are ready and tagged with a new version.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

To report a vulnerability:

1. Go to the repository's
   [Security tab](https://github.com/mirapalheta/lando/security) and click
   **"Report a vulnerability"** to open a private GitHub Security Advisory.
2. Include as much detail as you can: affected component, reproduction steps,
   potential impact, and any suggested fix.

You can expect an acknowledgement within 48 hours and a status update within
7 days. If a fix requires time to develop, we will coordinate a disclosure
timeline with you.

## Scope

The primary security surface areas of this project are:

- **HMAC signature verification** (`src/azure/Lando.Alexa.Core/Security/HMAC/`) —
  the mechanism that prevents unsigned or tampered Alexa directives from
  reaching the Azure Function.
- **LWA token handling** (`src/azure/Lando.Alexa.Core/Security/LWA/`) —
  the Login-with-Amazon OAuth2 flow and refresh token storage in Key Vault.
- **AWS Lambda signing** (`src/aws/lando-alexa-smart-home/src/hmac.ts`) —
  the outbound HMAC signer that pairs with the Azure verifier.
- **Terraform IAM / RBAC configuration** (`terraform/`) — the managed
  identity and Key Vault access policies.

Out-of-scope: issues in upstream dependencies (Azure Functions runtime,
Tailscale, Home Assistant, Alexa cloud) should be reported to those projects
directly.

## Security design summary

See [docs/architecture.md](docs/architecture.md#transport-security) for a
per-hop breakdown of the authentication and encryption strategy.
