using System.Globalization;
using MarcusRunge.AviationAgent.Core.Properties;
using MarcusRunge.Toolbox.Localization.Core;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>Formats a decoded TAF as a timeline and never derives an operational decision.</summary>
public static class TafFormatter
{
    private static readonly IReadOnlyDictionary<string, string> CloudAmountResourceKeys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["FEW"] = "MetarCloudAmountFEW",
        ["SCT"] = "MetarCloudAmountSCT",
        ["BKN"] = "MetarCloudAmountBKN",
        ["OVC"] = "MetarCloudAmountOVC",
        ["VV"] = "MetarCloudAmountVV",
        ["NSC"] = "MetarCloudAmountNSC",
        ["NCD"] = "MetarCloudAmountNCD",
        ["SKC"] = "MetarCloudAmountSKC",
        ["CLR"] = "MetarCloudAmountCLR",
    };

    public static string Format(DecodedTaf taf, AgentFocus focus, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(taf);
        ArgumentNullException.ThrowIfNull(culture);
        if (taf.IsCancelled) return string.Format(culture, Get("TafCancelled", culture), taf.Station);

        List<string> lines = [];
        if (focus is AgentFocus.Full or AgentFocus.Validity or AgentFocus.None) lines.Add(FormatHeader(taf, culture));
        if (focus is AgentFocus.Full or AgentFocus.None || Matches(taf.BaseForecast.Conditions, focus)) lines.Add(FormatGroup(taf.BaseForecast, focus, culture));
        foreach (TafForecastGroup group in taf.ChangeGroups.Where(group => focus is AgentFocus.Full or AgentFocus.Changes or AgentFocus.Validity or AgentFocus.WorstConditions or AgentFocus.None || Matches(group.Conditions, focus))) lines.Add(FormatGroup(group, focus, culture));
        return lines.Count == 0 ? Get("TafNoMatchingInformation", culture) : string.Join(Environment.NewLine, lines);
    }

    private static bool Matches(TafConditions conditions, AgentFocus focus) => focus switch
    {
        AgentFocus.Wind => conditions.Wind is not null,
        AgentFocus.Visibility => conditions.VisibilityMeters is not null || conditions.IsCavok,
        AgentFocus.Clouds => conditions.CloudLayers.Count > 0 || conditions.IsCavok,
        AgentFocus.Weather => conditions.WeatherPhenomena.Count > 0 || conditions.IsCavok,
        AgentFocus.Temperature or AgentFocus.Pressure => false,
        _ => true,
    };

    private static string FormatHeader(DecodedTaf taf, CultureInfo culture)
    {
        string flags = string.Join(", ", new[] { taf.IsAmended ? Get("TafAmended", culture) : null, taf.IsCorrected ? Get("TafCorrected", culture) : null }.OfType<string>());
        string header = string.Format(culture, Get("TafHeader", culture), taf.Station, FormatPoint(taf.IssueTime), FormatPeriod(taf.Validity));
        return string.IsNullOrEmpty(flags) ? header : $"{header} ({flags})";
    }

    private static string FormatGroup(TafForecastGroup group, AgentFocus focus, CultureInfo culture)
    {
        string label = group.Kind switch
        {
            TafChangeKind.Base => Get("TafGroupBase", culture),
            TafChangeKind.From => string.Format(culture, Get("TafGroupFrom", culture), FormatPoint(group.From!)),
            TafChangeKind.Becoming => string.Format(culture, Get("TafGroupBecoming", culture), FormatPeriod(group.Period!)),
            TafChangeKind.Temporary => string.Format(culture, Get("TafGroupTemporary", culture), FormatPeriod(group.Period!)),
            TafChangeKind.Probability => string.Format(culture, Get("TafGroupProbability", culture), group.ProbabilityPercent, FormatPeriod(group.Period!)),
            TafChangeKind.ProbabilityTemporary => string.Format(culture, Get("TafGroupProbabilityTemporary", culture), group.ProbabilityPercent, FormatPeriod(group.Period!)),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group.Kind, null),
        };
        string conditions = FormatConditions(group.Conditions, focus, culture);
        return $"{label}: {conditions}";
    }

    private static string FormatConditions(TafConditions conditions, AgentFocus focus, CultureInfo culture)
    {
        List<string> parts = [];
        if (focus is AgentFocus.Full or AgentFocus.Wind or AgentFocus.WorstConditions or AgentFocus.None && conditions.Wind is not null) parts.Add(FormatWind(conditions.Wind, culture));
        if (focus is AgentFocus.Full or AgentFocus.Visibility or AgentFocus.WorstConditions or AgentFocus.None && (conditions.VisibilityMeters is not null || conditions.IsCavok)) parts.Add(FormatVisibility(conditions, culture));
        if (focus is AgentFocus.Full or AgentFocus.Weather or AgentFocus.WorstConditions or AgentFocus.None && (conditions.WeatherPhenomena.Count > 0 || conditions.IsCavok)) parts.Add(FormatWeather(conditions, culture));
        if (focus is AgentFocus.Full or AgentFocus.Clouds or AgentFocus.WorstConditions or AgentFocus.None && (conditions.CloudLayers.Count > 0 || conditions.IsCavok)) parts.Add(FormatClouds(conditions, culture));
        if (focus is AgentFocus.Changes or AgentFocus.Validity) return Get("TafConditionsSeeRaw", culture);
        if (focus is AgentFocus.Temperature) return Get("TafTemperatureNotEncoded", culture);
        if (focus is AgentFocus.Pressure) return Get("TafPressureNotEncoded", culture);
        return parts.Count == 0 ? Get("TafNoMatchingInformation", culture) : string.Join("; ", parts);
    }

    private static string FormatWind(MetarWind? wind, CultureInfo culture)
    {
        if (wind is null) return Get("TafWindUnavailable", culture);
        string direction = wind.IsVariable ? Get("TafWindVariable", culture) : string.Format(culture, Get("TafWindDirection", culture), wind.DirectionDegrees);
        string value = string.Format(culture, Get("TafWindValue", culture), direction, wind.SpeedKnots);
        return wind.GustKnots is null ? value : string.Format(culture, Get("TafWindGusts", culture), value, wind.GustKnots);
    }

    private static string FormatVisibility(TafConditions conditions, CultureInfo culture) => conditions.IsCavok ? Get("TafCavok", culture) : conditions.VisibilityMeters >= 10_000 ? Get("TafVisibilityAtLeast", culture) : conditions.VisibilityMeters is null ? Get("TafVisibilityUnavailable", culture) : string.Format(culture, Get("TafVisibilityMeters", culture), conditions.VisibilityMeters);
    private static string FormatWeather(TafConditions conditions, CultureInfo culture) => conditions.IsCavok ? Get("TafNoSignificantWeather", culture) : conditions.WeatherPhenomena.Count == 0 ? Get("TafWeatherNone", culture) : string.Format(culture, Get("TafWeatherCodes", culture), string.Join(", ", conditions.WeatherPhenomena));
    private static string FormatClouds(TafConditions conditions, CultureInfo culture)
    {
        if (conditions.IsCavok) return Get("TafNoSignificantCloud", culture);
        if (conditions.CloudLayers.Count == 0) return Get("TafCloudsUnavailable", culture);
        return string.Join(", ", conditions.CloudLayers.Select(layer => FormatCloudLayer(layer, culture)));
    }

    private static string FormatCloudLayer(MetarCloudLayer layer, CultureInfo culture)
    {
        // METAR and TAF use the same cloud amount codes, so both formatters
        // intentionally share the existing localized cloud descriptions.
        string amount = CloudAmountResourceKeys.TryGetValue(layer.Amount, out string? resourceKey) ? Get(resourceKey, culture) : layer.Amount;
        if (layer.BaseFeetAboveAerodrome is null) return amount;

        string value = string.Format(culture, Get("MetarCloudLayer", culture), amount, layer.BaseFeetAboveAerodrome);
        return layer.CloudType is null ? value : string.Format(culture, Get("MetarCloudLayerWithType", culture), value, layer.CloudType);
    }
    private static string FormatPoint(TafTimePoint point) => $"{point.Day:00} {point.Hour:00}:{point.Minute:00} UTC";
    private static string FormatPeriod(TafPeriod period) => $"{FormatPoint(period.Start)} - {FormatPoint(period.End)}";
    private static string Get(string key, CultureInfo culture) => Resources.ResourceManager.GetString(key, culture) ?? $"[[{key}]]";
}
