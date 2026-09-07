using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarcusRunge.AviationAgent.Core;

public static partial class AgentDecisionParser
{
    public static AgentDecision Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        using JsonDocument document = JsonDocument.Parse(value, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3) throw new InvalidDataException("The model response must contain exactly action, station, and focus.");

        AgentAction action = ParseAction(root.GetProperty("action").GetString());
        AgentFocus focus = ParseFocus(root.GetProperty("focus").GetString());
        JsonElement stationElement = root.GetProperty("station");
        string? station = stationElement.ValueKind == JsonValueKind.Null ? null : stationElement.GetString();

        if (station is not null && !IcaoRegex().IsMatch(station)) throw new InvalidDataException($"Invalid ICAO station '{station}'.");
        if (action is AgentAction.GetMetar or AgentAction.GetTaf or AgentAction.GetMetarAndTaf && station is null) throw new InvalidDataException("Weather actions require an ICAO station.");
        if (action is AgentAction.Unknown or AgentAction.UnsupportedOperationalDecision && focus is not AgentFocus.None) throw new InvalidDataException($"Action '{action.ToWireValue()}' requires focus 'none'.");
        return new AgentDecision(action, station, focus);
    }

    private static AgentAction ParseAction(string? value) => value switch
    {
        "get_metar" => AgentAction.GetMetar, "get_taf" => AgentAction.GetTaf,
        "get_metar_and_taf" => AgentAction.GetMetarAndTaf,
        "explain_previous_result" => AgentAction.ExplainPreviousResult,
        "filter_previous_result" => AgentAction.FilterPreviousResult,
        "unsupported_operational_decision" => AgentAction.UnsupportedOperationalDecision,
        "unknown" => AgentAction.Unknown,
        _ => throw new InvalidDataException($"Unsupported action '{value}'."),
    };

    private static AgentFocus ParseFocus(string? value) => value switch
    {
        "full" => AgentFocus.Full, "wind" => AgentFocus.Wind, "visibility" => AgentFocus.Visibility,
        "clouds" => AgentFocus.Clouds, "weather" => AgentFocus.Weather, "temperature" => AgentFocus.Temperature,
        "pressure" => AgentFocus.Pressure, "validity" => AgentFocus.Validity, "changes" => AgentFocus.Changes,
        "worst_conditions" => AgentFocus.WorstConditions, "none" => AgentFocus.None,
        _ => throw new InvalidDataException($"Unsupported focus '{value}'."),
    };

    [GeneratedRegex("^[A-Z]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex IcaoRegex();
}
