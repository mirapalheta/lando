# Extending device types

This guide walks through adding support for a new Home Assistant domain. It
assumes you've read [architecture.md](architecture.md) — specifically the
**Entity transformation — Strategy** section — and have a working build.

The example domain is `vacuum`. The same pattern applies to any HA domain.

---

## How device type support works

Every supported HA domain needs exactly two transformer classes:

- A **discovery transformer** — tells Alexa what capabilities the device has.
- A **state transformer** — reports the device's current state.

Both transformers are registered under the domain name string (e.g. `"vacuum"`)
and resolved at runtime by the `EntityTransform` dispatcher. Entities whose
domain has no registered transformer are silently skipped at discovery time,
so a partial deploy never produces an invalid response.

---

## Step 1 — Choose the Alexa capability

Look up the Alexa Smart Home API docs for the closest capability to what you
want to expose. For a vacuum:

- `Alexa.PowerController` — turn the vacuum on (start cleaning) and off (return
  to dock).
- `Alexa.ModeController` — optionally expose cleaning modes if the vacuum
  supports them.

For this example we'll use `PowerController` only.

---

## Step 2 — Write the discovery transformer

Create `src/azure/Lando.Alexa.SmartHome/Transformers/Entity/VacuumDiscoveryTransformer.cs`:

```csharp
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Discovery transformer for the <c>vacuum</c> HA domain. Exposes the
/// device as a <c>VACUUM_CLEANER</c> with <c>Alexa.PowerController</c> —
/// "Alexa, turn on the robot" starts cleaning; "turn off" sends it home.
/// </summary>
public sealed class VacuumDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity)
        => DisplayCategory.VacuumCleaner;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        yield return Capability.PowerController;
    }
}
```

`DiscoveryTransformerBase` handles the endpoint id, friendly name, and the
mandatory `Alexa` / `Alexa.EndpointHealth` capabilities. Your override only
provides the per-domain additions.

---

## Step 3 — Write the state transformer

Create `src/azure/Lando.Alexa.SmartHome/Transformers/Entity/VacuumStateTransformer.cs`:

```csharp
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// State transformer for the <c>vacuum</c> HA domain. Reports
/// <c>Alexa.PowerController.powerState</c> — <c>ON</c> while the vacuum
/// is cleaning, <c>OFF</c> when docked, idle, or returning.
/// </summary>
public sealed class VacuumStateTransformer : StateTransformerBase
{
    // HA vacuum states that map to "the vacuum is actively running"
    private static readonly HashSet<string> ActiveStates =
        new(StringComparer.OrdinalIgnoreCase) { "cleaning", "spot_cleaning" };

    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        yield return new ContextProperty
        {
            Namespace  = Namespaces.PowerController,
            Name       = PowerControllerProperties.PowerState,
            Value      = ActiveStates.Contains(entity.State) ? PowerState.On : PowerState.Off,
            TimeOfSample           = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };
    }
}
```

**Rule:** every retrievable capability advertised in the discovery transformer
must have a corresponding property in the state transformer. For
`PowerController` that is exactly one property — `powerState`.

`StateTransformerBase` automatically prepends `Alexa.EndpointHealth` before
your domain properties, so you don't need to add it.

---

## Step 4 — Register both transformers

Open `src/azure/Lando.Alexa.SmartHome/Extensions/ServiceCollectionExtensions.cs`
and add one line alongside the other `AddEntityTransform` calls:

```csharp
services.AddEntityTransform<VacuumDiscoveryTransformer, VacuumStateTransformer>(Domains.Vacuum);
```

`Domains.Vacuum` is the constant `"vacuum"` defined in
`Lando.HomeAssistant.Abstractions/Constants.cs`. Using the constant rather than
a string literal ensures the registration key matches what
`HomeAssistantEntity.GetDomain()` returns for entities whose `entity_id` starts
with `vacuum.`.

---

## Step 5 — Handle control directives (optional)

`PowerController` directives (`TurnOn` / `TurnOff`) are already handled by the
existing `ControlDirectiveHandler<EmptyPayload>`. You need to add payload
transforms that map `TurnOn` → `vacuum.start` and `TurnOff` → `vacuum.return_to_base`.

Create `src/azure/Lando.Alexa.SmartHome/Transformers/Payload/VacuumTurnOnPayloadTransform.cs`:

```csharp
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Maps the <c>TurnOn</c> directive to <c>vacuum.start</c>.
/// </summary>
public sealed class VacuumTurnOnPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => HomeAssistantRequest.CallService("vacuum", "start", entity.EntityId);
}
```

And `VacuumTurnOffPayloadTransform.cs`:

```csharp
/// <summary>
/// Maps the <c>TurnOff</c> directive to <c>vacuum.return_to_base</c>.
/// </summary>
public sealed class VacuumTurnOffPayloadTransform : IPayloadTransform<EmptyPayload>
{
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => HomeAssistantRequest.CallService("vacuum", "return_to_base", entity.EntityId);
}
```

Register both in `ServiceCollectionExtensions.cs`:

```csharp
services.AddControlDirectiveHandler<EmptyPayload, VacuumTurnOnPayloadTransform>(DirectiveNames.TurnOn);
services.AddControlDirectiveHandler<EmptyPayload, VacuumTurnOffPayloadTransform>(DirectiveNames.TurnOff);
```

> **Note:** `TurnOn` and `TurnOff` are registered by domain-specific overrides,
> not globally — the existing `LightTurnOnPayloadTransform` et al. are keyed by
> directive name and resolved after the entity domain is confirmed by the
> discovery response. Check the existing registrations in `ServiceCollectionExtensions`
> if multiple domains share the same directive name to make sure you're registering
> the right overload.

---

## Step 6 — Write tests

Add a test class in `tests/Lando.Alexa.SmartHome.Tests/Transformers/Entity/`:

```csharp
public class VacuumTransformerTests
{
    private static HomeAssistantEntity MakeVacuum(string state, string name = "Robot Vacuum")
        => new() { EntityId = "vacuum.robot", State = state,
                   Attributes = new() { ["friendly_name"] = name } };

    [Theory]
    [InlineData("cleaning",      PowerState.On)]
    [InlineData("spot_cleaning", PowerState.On)]
    [InlineData("docked",        PowerState.Off)]
    [InlineData("idle",          PowerState.Off)]
    [InlineData("returning",     PowerState.Off)]
    [InlineData("unavailable",   PowerState.Off)]
    public void StateTransformer_MapsState(string haState, string expected)
    {
        var entity   = MakeVacuum(haState);
        var props    = new VacuumStateTransformer().Transform(entity);
        var power    = props.Single(p => p.Name == PowerControllerProperties.PowerState);
        Assert.Equal(expected, power.Value);
    }

    [Fact]
    public void DiscoveryTransformer_ProducesVacuumCleanerCategory()
    {
        var endpoint = new VacuumDiscoveryTransformer().Transform(MakeVacuum("docked"));
        Assert.Equal(DisplayCategory.VacuumCleaner, endpoint.DisplayCategories.First());
    }

    [Fact]
    public void DiscoveryTransformer_IncludesPowerControllerCapability()
    {
        var endpoint = new VacuumDiscoveryTransformer().Transform(MakeVacuum("docked"));
        Assert.Contains(endpoint.Capabilities, c => c.Interface == "Alexa.PowerController");
    }
}
```

---

## Checklist

- [ ] `VacuumDiscoveryTransformer.cs` created
- [ ] `VacuumStateTransformer.cs` created
- [ ] `AddEntityTransform<…>(Domains.Vacuum)` added to `ServiceCollectionExtensions`
- [ ] Payload transforms added if the domain needs non-generic control handling
- [ ] Test class covering state mapping and discovery shape
- [ ] `dotnet build` — zero warnings
- [ ] `dotnet test` — green

Once `dotnet build` and `dotnet test` pass, ask Alexa to "discover devices" and
the new domain's entities will appear.
