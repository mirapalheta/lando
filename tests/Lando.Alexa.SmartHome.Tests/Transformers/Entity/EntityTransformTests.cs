using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lando.Alexa.SmartHome.Transformers.Entity.Tests;

/// <summary>
/// <see cref="EntityTransform"/> resolves the per-domain transformer keyed by
/// entity domain and delegates -- these pin both closed forms
/// (<c>DiscoveryEndpoint</c> and <c>ContextProperty[]</c>) and the
/// no-transformer-registered null path.
/// </summary>
public class EntityTransformTests
{
    private static (EntityTransform sut, Mock<IKeyedServiceProvider> provider) Sut()
    {
        var provider = new Mock<IKeyedServiceProvider>();
        return (new EntityTransform(provider.Object), provider);
    }

    [Fact]
    public void Transform_DiscoveryEndpoint_delegates_to_keyed_transformer_for_domain()
    {
        var (sut, provider) = Sut();
        var endpoint = DiscoveryEndpoint.Create("light#kitchen", "Kitchen", DisplayCategory.Light, []);
        var discoveryTransformer = new Mock<IEntityTransform<DiscoveryEndpoint>>();
        discoveryTransformer.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>())).Returns(endpoint);
        provider.Setup(p => p.GetKeyedService(typeof(IEntityTransform<DiscoveryEndpoint>), "light"))
            .Returns(discoveryTransformer.Object);

        var entity = new HomeAssistantEntity { EntityId = "light.kitchen" };
        var result = ((IEntityTransform<DiscoveryEndpoint>)sut).Transform(entity);

        result.ShouldBeSameAs(endpoint);
    }

    [Fact]
    public void Transform_ContextProperties_delegates_to_keyed_transformer_for_domain()
    {
        var (sut, provider) = Sut();
        ContextProperty[] props = [new() { Namespace = "Alexa.PowerController", Name = "powerState", Value = "ON" }];
        var stateTransformer = new Mock<IEntityTransform<ContextProperty[]>>();
        stateTransformer.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>())).Returns(props);
        provider.Setup(p => p.GetKeyedService(typeof(IEntityTransform<ContextProperty[]>), "switch"))
            .Returns(stateTransformer.Object);

        var entity = new HomeAssistantEntity { EntityId = "switch.outlet" };
        var result = ((IEntityTransform<ContextProperty[]>)sut).Transform(entity);

        result.ShouldBeSameAs(props);
    }

    [Fact]
    public void Transform_ReturnsNull_WhenNoTransformerRegisteredForDomain()
    {
        // Loose mock (default behavior): an unconfigured GetKeyedService call
        // returns null, exercising the "no transformer registered" path.
        var (sut, _) = Sut();

        var entity = new HomeAssistantEntity { EntityId = "vacuum.roomba" };
        var result = ((IEntityTransform<DiscoveryEndpoint>)sut).Transform(entity);

        result.ShouldBeNull();
    }
}
