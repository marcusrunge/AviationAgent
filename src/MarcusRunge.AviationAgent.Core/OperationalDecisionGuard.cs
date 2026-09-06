using System.Text.RegularExpressions;

namespace MarcusRunge.AviationAgent.Core;

public static partial class OperationalDecisionGuard
{
    public static AgentDecision? TryReject(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        if (!OperationalDecisionRegex().IsMatch(input))
        {
            return null;
        }

        string? station = ExtractStation(input);

        return new AgentDecision("unsupported_operational_decision", station, "none");
    }

    private static string? ExtractStation(string input)
    {
        // ICAO codes in operational questions normally follow a location
        // preposition. This prevents ordinary four-letter words such as
        // "kann", "darf" or "Flug" from being interpreted as stations.
        Match locationMatch = ContextualIcaoStationRegex().Match(input);

        if (locationMatch.Success)
        {
            return locationMatch.Groups["station"].Value.ToUpperInvariant();
        }

        // An explicitly labelled ICAO code remains valid even when the
        // surrounding sentence does not use a location preposition.
        Match labelledMatch = LabelledIcaoStationRegex().Match(input);

        return labelledMatch.Success ? labelledMatch.Groups["station"].Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex(@"\b(sicher\b.{0,80}\b(starten|landen)|(starten|landen)\b.{0,80}\bsicher|darf\s+ich\b.{0,80}\b(fliegen|starten|landen)|kann\s+ich\b.{0,80}\b(fliegen|starten|landen)|soll\s+ich\b.{0,80}\b(fliegen|starten|landen)|flug\b.{0,80}\b(freigeben|frei)|start\b.{0,80}\b(freigeben|frei)|landung\b.{0,80}\b(freigeben|frei)|go[\s/-]?no[\s/-]?go|zulässig|persönliche[nr]?\s+minima|meine[nr]?\s+minima|ausweichflugplatz|alternate|landung\s+möglich|start\s+möglich|flugentscheidung)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OperationalDecisionRegex();

    [GeneratedRegex(@"\b(?:in|nach|auf|für|von|ab)\s+(?<station>[A-Z]{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContextualIcaoStationRegex();

    [GeneratedRegex(@"\b(?:ICAO|Flugplatz|Flughafen|Station)\s*[:\-]?\s*(?<station>[A-Z]{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LabelledIcaoStationRegex();
}