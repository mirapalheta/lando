# Diagram sources

PlantUML (`*.puml`) and draw.io (`*.drawio`) sources for the diagrams referenced
from [`docs/architecture.md`](../../architecture.md). Rendered output lives one
level up under `docs/assets/`.

## Layout

| Source                          | Rendered output                         | Subject                                                                       |
| ------------------------------- | --------------------------------------- | ----------------------------------------------------------------------------- |
| `01-context.puml`               | `01-context.svg` / `01-context.png`     | C4 System Context — Alexa user, Alexa cloud, AWS, Azure, Tailscale, HA.       |
| `02-container.puml`             | `02-container.svg` / `02-container.png` | C4 Container — projects inside the Azure Function and their internal flow.    |
| `03-seq-discover.puml`          | `03-seq-discover.svg`                   | Sequence: `Alexa.Discovery.Discover` end-to-end.                              |
| `04-seq-control.puml`           | `04-seq-control.svg`                    | Sequence: `PowerController.TurnOn`.                                           |
| `05-seq-change-report.puml`     | `05-seq-change-report.svg`              | Sequence: proactive `Alexa.ChangeReport` driven by the HA WebSocket.          |
| `06-class-transformers.puml`    | `06-class-transformers.svg`             | Class diagram: `IEntityTransform` Strategy + concrete transformers.           |
| `07-class-directive-handlers.puml` | `07-class-directive-handlers.svg`    | Class diagram: `DirectiveHandler` Chain of Responsibility dispatch.           |
| `08-deployment.drawio`          | `08-deployment.svg` / `08-deployment.png` | Deployment topology: AWS region, Azure region, home LAN, Tailscale overlay. |
| `09-hmac-flow.puml`             | `09-hmac-flow.svg`                      | Sequence: HMAC signing on the Lambda + verification on the Function.          |

## Rendering

Both PlantUML and draw.io sources render to SVG + PNG via the repo-root
script `scripts/render-diagrams.sh`:

```bash
brew install plantuml
brew install --cask drawio
./scripts/render-diagrams.sh    # from repo root
```

The draw.io source is edited in [diagrams.net](https://app.diagrams.net/) (or
the VS Code "Draw.io Integration" extension); `render-diagrams.sh` re-exports
both SVG and PNG.

Both source and rendered output are committed so a reader who only browses
GitHub can see the diagrams without running anything, and a contributor who
wants to tweak them can edit the source.
