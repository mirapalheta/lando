using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

/// <summary>
/// Discovery coverage for the <c>scene</c> and <c>script</c> domains, both of
/// which surface as <c>Alexa.SceneController</c> endpoints. Pins the display
/// categories (SCENE_TRIGGER vs ACTIVITY_TRIGGER), the deactivation flag
/// (scenes are fire-only, scripts are stoppable), and the stateless reporting.
/// </summary>
public class SceneScriptDiscoveryTests
{
    private readonly SceneDiscoveryTransformer _scene = new();
    private readonly ScriptDiscoveryTransformer _script = new();
    private readonly SceneControllerStateTransformer _state = new();

    [Fact]
    public void Scene_advertises_SceneController_and_SceneTrigger_category()
    {
        var endpoint = _scene.Transform(TestEntities.Scene());

        endpoint.Capabilities.Select(c => c.Interface).ShouldContain(Namespaces.SceneController);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.SceneTrigger);
    }

    [Fact]
    public void Scene_is_fire_only_and_synchronous()
    {
        var cap = _scene.Transform(TestEntities.Scene())
            .Capabilities.Single(c => c.Interface == Namespaces.SceneController);

        cap.SupportsDeactivation.ShouldBe(false);
        cap.ProactivelyReported.ShouldBe(false);
    }

    [Fact]
    public void Script_advertises_SceneController_and_ActivityTrigger_category()
    {
        var endpoint = _script.Transform(TestEntities.Script());

        endpoint.Capabilities.Select(c => c.Interface).ShouldContain(Namespaces.SceneController);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.ActivityTrigger);
    }

    [Fact]
    public void Script_supports_deactivation()
    {
        var cap = _script.Transform(TestEntities.Script())
            .Capabilities.Single(c => c.Interface == Namespaces.SceneController);

        cap.SupportsDeactivation.ShouldBe(true);
    }

    [Fact]
    public void Neither_advertises_PowerController()
    {
        _scene.Transform(TestEntities.Scene()).Capabilities
            .Select(c => c.Interface).ShouldNotContain(Namespaces.PowerController);
        _script.Transform(TestEntities.Script()).Capabilities
            .Select(c => c.Interface).ShouldNotContain(Namespaces.PowerController);
    }

    [Fact]
    public void SceneController_endpoints_report_only_endpoint_health()
    {
        var props = _state.Transform(TestEntities.Script());

        props.ShouldHaveSingleItem();
        props.Single().Namespace.ShouldBe(Namespaces.EndpointHealth);
    }
}
