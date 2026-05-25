# FAQ

## Why Tailscale instead of exposing Home Assistant directly?

Exposing HA directly requires either a static public IP, a dynamic DNS service,
or a reverse proxy — all of which open your home network to the internet. A
misconfigured rule or a HA vulnerability would give an attacker direct access
to your home's automation system.

Tailscale creates an encrypted WireGuard overlay network. The Azure container
app's Tailscale sidecar joins your network and reaches HA as if it were a
machine on your LAN, but nothing on your router is listening for inbound
connections. There is no open port to probe.

---

## Why not Nabu Casa?

[Nabu Casa](https://www.nabucasa.com/) is the right answer for most people —
it's official, it's maintained by the HA team, and it handles the cloud
plumbing so you don't have to. If you're happy paying ~$6.50/month and
trusting a third-party cloud with your HA data, use it.

Lando is for people who want:

- No third-party cloud in the path between Alexa and their home network.
- Full visibility into the code that translates directives to HA service calls.
- A bill that comes from their own Azure subscription, itemised, with no
  subscription dependency on a single vendor.

Also, honestly? I just really wanted to build this myself. There's something
satisfying about "Alexa, turn off the lights" working end-to-end through code
you wrote from scratch 🙃

---

## What does it cost to run?

Lando runs on **Azure Container Apps** (Consumption plan) and **AWS Lambda** —
both billed purely on usage, with no always-on compute cost.

Approximate monthly cost for typical home use (~few hundred Alexa commands/day):

| Resource                                | Cost             |
| --------------------------------------- | ---------------- |
| Azure Container Apps (Consumption)      | ~$1–3            |
| Azure Container Registry (Basic)        | ~$5.00           |
| Azure Key Vault                         | ~$0.60           |
| Azure Storage Account (Tailscale state) | ~$0.50           |
| Azure Log Analytics                     | ~$0–2            |
| AWS Lambda + Secrets Manager            | <$1.00           |
| **Total**                               | **~$8–12/month** |

Container Apps Consumption pricing is per vCPU-second and GiB-second of active
use, so the actual cost scales with how often Alexa calls the bridge. Light use
(a few dozen commands per day) will be at the low end; heavy use or proactive
ChangeReport traffic will push it higher.

---

## Why two clouds? Why not just Azure?

The Alexa Smart Home API calls a Lambda endpoint — that's the only endpoint
format Alexa's Smart Home gateway supports. You can't point it at an arbitrary
HTTPS URL; it must be a Lambda ARN (or an HTTPS endpoint with Alexa verification,
which requires the same certificate-pinning dance Lando already does on the
Azure side).

The Lambda is intentionally tiny: it receives the directive, signs it with
HMAC, and forwards it. All the logic lives in Azure. If you later want to move
the Azure Function to a different host, you only change the `AZURE_ENDPOINT`
Lambda environment variable.

---

## Why Azure Functions instead of a bare VM or container?

Azure Functions on a B1 App Service Plan gives you a process that's always
running (no cold starts) with managed deployment, TLS termination, Application
Insights integration, and Key Vault references — all without managing a VM,
OS patches, or a reverse proxy. The code is also fully unit-testable without
standing up any Azure infrastructure.

---

## Does this work with Alexa routines and groups?

Yes. Lando implements the standard Alexa Smart Home API — discovery,
control directives, state reports, and proactive ChangeReports. Alexa treats
the exposed endpoints the same as any other smart home device, so routines,
groups, scenes, and the Alexa app's device dashboard all work normally.

---

## How many devices can Lando handle?

The Alexa Smart Home API supports up to 300 endpoints per skill. The function
streams all supported HA entities through the discovery transformer on each
`Discover` directive — there's no pre-filter. If you have more than 300
supported entities you'll need to add a filter (for example, exposing only
entities that have a specific HA label or area) in `DiscoverDirectiveHandler`.

---

## Can I add support for a device type that isn't listed?

Yes — see [extending-device-types.md](extending-device-types.md) for a
complete worked example. The transformer pattern is designed so adding a new
HA domain is two new files and two lines of registration, with no changes to
existing code.

---

## Is it safe to run `AcceptGrant` more than once?

Yes. Each time a user enables the Alexa skill, Alexa calls `AcceptGrant` with
a fresh authorization code. Lando exchanges it for a refresh token and stores
it in Key Vault under a key derived from the LWA `user_id`. Running the grant
flow again just overwrites the existing token for that user — it doesn't create
duplicate entries or revoke earlier tokens.

---

## What happens if Azure or AWS goes down?

Voice commands will fail with "that device isn't responding" until the
affected service recovers. Local HA automations are not affected — Lando is
only in the path for Alexa directives. HA's own automations, dashboards, and
local integrations continue to work normally.
