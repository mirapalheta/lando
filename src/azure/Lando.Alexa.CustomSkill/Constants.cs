namespace Lando.Alexa.CustomSkill;

/// <summary>
/// Constants for the Alexa Custom Skill (intent) path: the Azure Functions
/// trigger metadata + DI key, and the Home Assistant entity attributes that
/// opt a script into voice-intent routing.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Azure Functions trigger metadata for the custom-skill endpoint.
    /// </summary>
    public static class Function
    {
        /// <summary>
        /// Function name used by the <c>[Function]</c> attribute.
        /// </summary>
        public const string Name = "Alexa-CustomSkill";

        /// <summary>
        /// DI key under which the intent handler + its HMAC validator are registered.
        /// </summary>
        public const string Handler = "IntentSkillHandler";

        /// <summary>
        /// Relative HTTP route the custom-skill endpoint listens on.
        /// </summary>
        public const string Route = "alexa/custom-skill";
    }

    /// <summary>
    /// HA entity attributes that drive intent routing. A script opts in by
    /// setting <see cref="Intent"/> to the Alexa intent name it handles; the
    /// optional <see cref="Slots"/> map translates Alexa slot names to the
    /// script's field names.
    /// </summary>
    public static class CustomAttributes
    {
        /// <summary>
        /// Alexa intent name this script handles (e.g. <c>RunRoutine</c>).
        /// </summary>
        public const string Intent = "alexa_intent";

        /// <summary>
        /// Map of <c>{ alexa_slot_name: script_field_name }</c>.
        /// </summary>
        public const string Slots = "alexa_slots";
    }
}
