# Contributing to Lando

Thank you for your interest in contributing. This document covers the process
for reporting bugs, proposing changes, and getting code merged.

## Before you open an issue or PR

- Search existing issues and pull requests to avoid duplicates.
- For security vulnerabilities, follow the [SECURITY.md](SECURITY.md) process
  instead of opening a public issue.
- For questions ("how do I configure X?"), use the
  [question issue template](.github/ISSUE_TEMPLATE/question.yml) or start a
  GitHub Discussion if discussions are enabled.

## Development setup

```bash
# Clone and restore dependencies
git clone https://github.com/mirapalheta/lando.git
cd lando
dotnet restore

# Build
dotnet build

# Run all tests
dotnet test

# AWS Lambda proxy (Node 26 required)
cd src/aws/lando-alexa-proxy
npm ci
npm test
```

Local Azure Function development requires
[Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
and [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
for the local storage emulator.

See [docs/getting-started.md](docs/getting-started.md) for the full local
development walkthrough.

## Commit conventions

Lando follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

Common types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `ci`.

Scopes mirror the project names: `alexa-core`, `alexa-smarthome`, `ha-core`,
`functionapp`, `lambda`, `terraform`, `docs`.

Examples:

- `feat(alexa-smarthome): add vacuum domain transformer`
- `fix(ha-core): retry WebSocket connection on transient disconnect`
- `docs: add FAQ entry for Nabu Casa comparison`

## Branch policy

- **`main`** is always deployable. Direct pushes are blocked; all changes go
  through a pull request.
- Branch names: `feat/<short-description>`, `fix/<short-description>`,
  `chore/<short-description>`.

## Pull request checklist

The PR template will remind you, but the key points are:

- All new behaviour is covered by tests (`dotnet test` and `npm test` pass).
- `dotnet build` produces zero warnings at the `TreatWarningsAsErrors` level
  (enforced in `Directory.Build.props`).
- New public APIs have XML documentation comments.
- No personal hostnames, IPs, account IDs, or resource names introduced.

## Adding a new Home Assistant device type

[docs/extending-device-types.md](docs/extending-device-types.md) has a
complete worked example — two transformer files, a registration line, and the
matching test class.

## Code style

C# style is enforced via `.editorconfig` and `EnforceCodeStyleInBuild: true` in
`Directory.Build.props`. JavaScript/TypeScript style is enforced via ESLint and
Prettier (run `npm run lint` in the Lambda project). Terraform is formatted with
`terraform fmt`.

## Code of Conduct

This project follows the [Contributor Covenant v2.1](CODE_OF_CONDUCT.md).
Participation in this project implies acceptance of its terms.
