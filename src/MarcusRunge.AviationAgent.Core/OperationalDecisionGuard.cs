using System.Text.RegularExpressions;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Rejects requests that require an operational flight decision.
/// </summary>
public static partial class OperationalDecisionGuard
{
    private static readonly string[] PermissionIntroductions =
    [
        "kann ich",
        "darf ich",
        "soll ich",
    ];

    private static readonly string[] FlightOperations =
    [
        "fliegen",
        "abfliegen",
        "starten",
        "landen",
    ];

    private static readonly string[] OperationalKeywords =
    [
        "go-no-go",
        "go/no-go",
        "go no go",
        "flugentscheidung",
        "ausweichflugplatz",
        "alternate",
        "meine minima",
        "meinen minima",
        "meiner minima",
        "persönliche minima",
        "persönlichen minima",
    ];

    /// <summary>
    /// Returns a deterministic rejection when the request asks for an
    /// operational flight decision.
    /// </summary>
    /// <param name="input">The natural-language user input.</param>
    /// <returns>
    /// A rejection decision when an operational request was detected;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    public static AgentDecision? TryReject(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        if (!IsOperationalDecisionRequest(input))
        {
            return null;
        }

        return new AgentDecision(AgentAction.UnsupportedOperationalDecision, ExtractStation(input), AgentFocus.None);
    }

    private static bool IsOperationalDecisionRequest(string input)
    {
        string normalizedInput = Normalize(input);

        // Permission questions combine a personal decision request with a
        // flight operation and must never be delegated to the language model.
        bool isPermissionRequest = PermissionIntroductions.Any(normalizedInput.Contains) && FlightOperations.Any(normalizedInput.Contains);

        if (isPermissionRequest)
        {
            return true;
        }

        // Explicit safety wording combined with an operation represents the
        // same unsupported decision even without "kann", "darf", or "soll".
        bool isSafetyRequest = normalizedInput.Contains("sicher") && FlightOperations.Any(normalizedInput.Contains);

        if (isSafetyRequest)
        {
            return true;
        }

        if (OperationalKeywords.Any(normalizedInput.Contains))
        {
            return true;
        }

        return IsApprovalRequest(normalizedInput);
    }

    private static bool IsApprovalRequest(string normalizedInput)
    {
        bool mentionsFlightOperation = normalizedInput.Contains("flug") || normalizedInput.Contains("start") || normalizedInput.Contains("landung") || normalizedInput.Contains("anflug");

        bool requestsApproval =
            normalizedInput.Contains("freigeben") || normalizedInput.Contains("freigegeben") || normalizedInput.Contains("frei") || normalizedInput.Contains("zulässig") || normalizedInput.Contains("möglich");

        return mentionsFlightOperation && requestsApproval;
    }

    private static string? ExtractStation(string input)
    {
        // ICAO codes are accepted only in a plausible location context.
        // This prevents ordinary four-letter words such as "kann", "darf",
        // and "Flug" from being interpreted as station identifiers.
        Match contextualMatch = ContextualIcaoStationRegex().Match(input);

        if (contextualMatch.Success)
        {
            return NormalizeStation(contextualMatch);
        }

        // Explicit labels provide an additional unambiguous extraction path.
        Match labelledMatch = LabelledIcaoStationRegex().Match(input);

        return labelledMatch.Success ? NormalizeStation(labelledMatch) : null;
    }

    private static string NormalizeStation(Match match) => match.Groups["station"].Value.ToUpperInvariant();

    private static string Normalize(string input) => string.Join(' ', input.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"\b(?:in|nach|auf|für|von|ab)\s+(?:(?:dem|den|der|die|das)\s+)?(?:(?:flug|flugplatz|flughafen|station|icao)\s+)?(?<station>[A-Z]{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContextualIcaoStationRegex();

    [GeneratedRegex(@"\b(?:ICAO|Flugplatz|Flughafen|Station)\s*[:-]?\s*(?<station>[A-Z]{4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LabelledIcaoStationRegex();
}