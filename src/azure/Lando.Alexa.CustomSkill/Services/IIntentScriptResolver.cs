using System.Threading;
using System.Threading.Tasks;

namespace Lando.Alexa.CustomSkill.Services;

/// <summary>
/// Resolves the HA script bound to a given Alexa intent name, from the
/// <c>alexa_intent</c> / <c>alexa_slots</c> attributes on exposed scripts.
/// </summary>
public interface IIntentScriptResolver
{
    /// <summary>
    /// Returns the script bound to <paramref name="intentName"/>, or null if none.
    /// </summary>
    Task<IntentScript?> ResolveAsync(string intentName, CancellationToken cancellationToken);
}
