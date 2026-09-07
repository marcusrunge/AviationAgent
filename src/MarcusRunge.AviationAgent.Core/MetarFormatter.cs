using System.Globalization;
using MarcusRunge.AviationAgent.Core.Properties;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Formats deterministically decoded METAR data without making an operational assessment.
/// </summary>
public static class MetarFormatter
{
    private static readonly IReadOnlyDictionary<string, string> WeatherResourceKeys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RA"] = "MetarWeatherRain",
        ["SN"] = "MetarWeatherSnow",
        ["DZ"] = "MetarWeatherDrizzle",
        ["BR"] = "MetarWeatherMist",
        ["FG"] = "MetarWeatherFog",
        ["TS"] = "MetarWeatherThunderstorm",
        ["GR"] = "MetarWeatherHail",
        ["GS"] = "MetarWeatherSmallHail",
        ["HZ"] = "MetarWeatherHaze",
    };

    public static string Format(DecodedMetar metar, AgentFocus focus, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(metar);
        ArgumentNullException.ThrowIfNull(culture);

        return focus switch
        {
            AgentFocus.Wind => FormatWind(metar, culture),
            AgentFocus.Visibility => FormatVisibility(metar, culture),
            AgentFocus.Clouds => FormatClouds(metar, culture),
            AgentFocus.Weather => FormatWeather(metar, culture),
            AgentFocus.Temperature => FormatTemperature(metar, culture),
            AgentFocus.Pressure => FormatPressure(metar, culture),
            AgentFocus.Validity => FormatObservationTime(metar, culture),
            AgentFocus.Changes => Get("MetarNoChangeInformation", culture),
            AgentFocus.WorstConditions => FormatWorstConditions(metar, culture),
            AgentFocus.Full or AgentFocus.None => FormatFull(metar, culture),
            _ => throw new ArgumentOutOfRangeException(nameof(focus), focus, null),
        };
    }

    private static string FormatFull(DecodedMetar metar, CultureInfo culture) => string.Join(Environment.NewLine,
    [
        string.Format(culture, Get("MetarStation", culture), metar.Station),
        FormatObservationTime(metar, culture),
        FormatWind(metar, culture),
        FormatVisibility(metar, culture),
        FormatWeather(metar, culture),
        FormatClouds(metar, culture),
        FormatTemperature(metar, culture),
        FormatPressure(metar, culture),
    ]);

    private static string FormatObservationTime(DecodedMetar metar, CultureInfo culture) => string.Format(culture, Get("MetarObservationTime", culture), metar.ObservationDay, metar.ObservationTimeUtc.Hour, metar.ObservationTimeUtc.Minute);

    private static string FormatWind(DecodedMetar metar, CultureInfo culture)
    {
        if (metar.Wind is null) return string.Format(culture, Get("MetarWind", culture), Get("MetarValueUnavailable", culture));

        string direction = metar.Wind.IsVariable ? Get("MetarWindVariable", culture) : string.Format(culture, Get("MetarWindDirection", culture), metar.Wind.DirectionDegrees);
        string value = string.Format(culture, Get("MetarWindValue", culture), direction, metar.Wind.SpeedKnots);
        if (metar.Wind.GustKnots is not null) value = string.Format(culture, Get("MetarWindWithGusts", culture), value, metar.Wind.GustKnots);
        return string.Format(culture, Get("MetarWind", culture), value);
    }

    private static string FormatVisibility(DecodedMetar metar, CultureInfo culture)
    {
        string value = metar.IsCavok
            ? Get("MetarVisibilityCavok", culture)
            : metar.VisibilityMeters is null
                ? Get("MetarValueUnavailable", culture)
                : metar.VisibilityMeters >= 10_000
                    ? Get("MetarVisibilityAtLeastTenKilometers", culture)
                    : string.Format(culture, Get("MetarVisibilityMeters", culture), metar.VisibilityMeters);
        return string.Format(culture, Get("MetarVisibility", culture), value);
    }

    private static string FormatWeather(DecodedMetar metar, CultureInfo culture)
    {
        string value = metar.WeatherPhenomena.Count == 0 ? Get("MetarWeatherNone", culture) : string.Join(", ", metar.WeatherPhenomena.Select(code => TranslateWeather(code, culture)));
        return string.Format(culture, Get("MetarWeather", culture), value);
    }

    private static string FormatClouds(DecodedMetar metar, CultureInfo culture)
    {
        string value = metar.CloudLayers.Count == 0 ? Get("MetarCloudsUnavailable", culture) : string.Join(", ", metar.CloudLayers.Select(layer => FormatCloudLayer(layer, culture)));
        return string.Format(culture, Get("MetarClouds", culture), value);
    }

    private static string FormatTemperature(DecodedMetar metar, CultureInfo culture)
    {
        string temperature = metar.TemperatureCelsius is null ? Get("MetarValueUnavailable", culture) : string.Format(culture, Get("MetarDegreesCelsius", culture), metar.TemperatureCelsius);
        string dewPoint = metar.DewPointCelsius is null ? Get("MetarValueUnavailable", culture) : string.Format(culture, Get("MetarDegreesCelsius", culture), metar.DewPointCelsius);
        return string.Format(culture, Get("MetarTemperature", culture), temperature, dewPoint);
    }

    private static string FormatPressure(DecodedMetar metar, CultureInfo culture)
    {
        string value = metar.QnhHectopascals is null ? Get("MetarValueUnavailable", culture) : string.Format(culture, Get("MetarPressureHectopascals", culture), metar.QnhHectopascals);
        return string.Format(culture, Get("MetarPressure", culture), value);
    }

    private static string FormatWorstConditions(DecodedMetar metar, CultureInfo culture) => string.Join(Environment.NewLine, [FormatWind(metar, culture), FormatVisibility(metar, culture), FormatWeather(metar, culture), FormatClouds(metar, culture)]);

    private static string FormatCloudLayer(MetarCloudLayer layer, CultureInfo culture)
    {
        string amount = Get($"MetarCloudAmount{layer.Amount}", culture);
        if (layer.BaseFeetAboveAerodrome is null) return amount;

        string value = string.Format(culture, Get("MetarCloudLayer", culture), amount, layer.BaseFeetAboveAerodrome);
        return layer.CloudType is null ? value : string.Format(culture, Get("MetarCloudLayerWithType", culture), value, layer.CloudType);
    }

    private static string TranslateWeather(string code, CultureInfo culture)
    {
        string intensity = code.StartsWith('+') ? Get("MetarWeatherHeavy", culture) : code.StartsWith('-') ? Get("MetarWeatherLight", culture) : string.Empty;
        string normalizedCode = code.TrimStart('+', '-');
        List<string> descriptions = [];

        // METAR weather groups can combine descriptors and phenomena. Known
        // fragments are translated while the original code remains visible
        // when the supported subset cannot describe the complete group.
        foreach ((string fragment, string resourceKey) in WeatherResourceKeys)
        {
            if (normalizedCode.Contains(fragment, StringComparison.Ordinal)) descriptions.Add(Get(resourceKey, culture));
        }

        if (descriptions.Count == 0) return code;
        string description = string.Join(" ", descriptions.Distinct(StringComparer.Ordinal));
        return string.IsNullOrEmpty(intensity) ? $"{description} ({code})" : $"{intensity} {description} ({code})";
    }

    private static string Get(string key, CultureInfo culture) => Resources.ResourceManager.GetString(key, culture) ?? $"[[{key}]]";
}
