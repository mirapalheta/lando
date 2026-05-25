namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Alexa unit of measure constants. These are used in the <c>unitOfMeasure</c> field of capability configuration objects, and in the <c>scale</c> field of temperature properties.
/// </summary>
public static class Units
{
    public const string Percent = "Alexa.Unit.Percent";

    public static class Temperature
    {
        public const string Celsius = "Alexa.Unit.Temperature.Celsius";
        public const string Fahrenheit = "Alexa.Unit.Temperature.Fahrenheit";
        public const string Degrees = "Alexa.Unit.Temperature.Degrees";
    }
    public static class Distance
    {
        public const string Inches = "Alexa.Unit.Distance.Inches";
        public const string Centimeters = "Alexa.Unit.Distance.Centimeters";
        public const string Meters = "Alexa.Unit.Distance.Meters";
    }
    public static class Volume
    {
        public const string Liters = "Alexa.Unit.Volume.Liters";
        public const string Gallons = "Alexa.Unit.Volume.Gallons";
        public const string CubicMeters = "Alexa.Unit.Volume.CubicMeters";
    }
    public static class Mass
    {
        public const string Pounds = "Alexa.Unit.Mass.Pounds";
        public const string Kilograms = "Alexa.Unit.Mass.Kilograms";
        public const string Grams = "Alexa.Unit.Mass.Grams";
    }
    public static class Speed
    {
        public const string MilesPerHour = "Alexa.Unit.Speed.MilesPerHour";
        public const string MetersPerSecond = "Alexa.Unit.Speed.MetersPerSecond";
    }
}
