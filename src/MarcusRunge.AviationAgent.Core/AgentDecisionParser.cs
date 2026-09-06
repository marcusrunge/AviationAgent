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
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3)
            throw new InvalidDataException("The model response must be a JSON object with exactly action, station, and focus.");

        string action = root.GetProperty("action").GetString() ?? throw new InvalidDataException("Action is required.");
        string focus = root.GetProperty("focus").GetString() ?? throw new InvalidDataException("Focus is required.");
        JsonElement stationElement = root.GetProperty("station");
        string? station = stationElement.ValueKind == JsonValueKind.Null ? null : stationElement.GetString();

        if (!AgentContract.Actions.Contains(action)) throw new InvalidDataException($"Unsupported action '{action}'.");
        if (!AgentContract.Focuses.Contains(focus)) throw new InvalidDataException($"Unsupported focus '{focus}'.");
        if (station is not null && !IcaoRegex().IsMatch(station)) throw new InvalidDataException($"Invalid ICAO station '{station}'.");
        if (action is "get_metar" or "get_taf" or "get_metar_and_taf" && station is null) throw new InvalidDataException("Weather actions require an ICAO station.");
        if (action is "unknown" or "unsupported_operational_decision" && focus != "none") throw new InvalidDataException($"Action '{action}' requires focus 'none'.");

        return new AgentDecision(action, station, focus);
    }

    [GeneratedRegex("^[A-Z]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex IcaoRegex();
}
