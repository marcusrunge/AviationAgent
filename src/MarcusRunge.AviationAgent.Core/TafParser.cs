using System.Globalization;
using System.Text.RegularExpressions;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>Decodes the operationally relevant structure of an international TAF without assessing flight safety.</summary>
public static partial class TafParser
{
    public static DecodedTaf Parse(string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);
        string normalized = rawText.Trim().TrimEnd('=').Trim();
        string[] tokens = normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        if (index < tokens.Length && tokens[index].Equals("TAF", StringComparison.OrdinalIgnoreCase)) index++;

        bool isAmended = ConsumeToken(tokens, ref index, "AMD");
        bool isCorrected = ConsumeToken(tokens, ref index, "COR");
        if (tokens.Length < index + 3) throw new InvalidDataException("The TAF does not contain station, issue time, and validity.");

        string station = tokens[index++].ToUpperInvariant();
        if (!StationRegex().IsMatch(station)) throw new InvalidDataException($"Invalid TAF station '{station}'.");
        TafTimePoint issueTime = ParseIssueTime(tokens[index++]);
        TafPeriod validity = ParsePeriod(tokens[index++]);
        bool isCancelled = index < tokens.Length && tokens[index].Equals("CNL", StringComparison.OrdinalIgnoreCase);
        if (isCancelled) return new DecodedTaf(normalized, station, issueTime, validity, isAmended, isCorrected, true, CreateEmptyBase(validity), []);

        List<string> baseTokens = [];
        while (index < tokens.Length && !IsChangeStart(tokens[index])) baseTokens.Add(tokens[index++]);
        TafForecastGroup baseForecast = new(TafChangeKind.Base, validity, null, null, ParseConditions(baseTokens));
        List<TafForecastGroup> changes = [];

        while (index < tokens.Length)
        {
            string marker = tokens[index++].ToUpperInvariant();
            TafChangeKind kind;
            TafPeriod? period = null;
            TafTimePoint? from = null;
            int? probability = null;

            if (marker.StartsWith("FM", StringComparison.Ordinal)) { kind = TafChangeKind.From; from = ParseFrom(marker); }
            else if (marker is "BECMG" or "TEMPO") { kind = marker == "BECMG" ? TafChangeKind.Becoming : TafChangeKind.Temporary; period = RequirePeriod(tokens, ref index, marker); }
            else if (marker is "PROB30" or "PROB40")
            {
                probability = ParseNumber(marker[4..]);
                bool hasTempo = index < tokens.Length && tokens[index].Equals("TEMPO", StringComparison.OrdinalIgnoreCase);
                if (hasTempo) index++;
                kind = hasTempo ? TafChangeKind.ProbabilityTemporary : TafChangeKind.Probability;
                period = RequirePeriod(tokens, ref index, marker);
            }
            else throw new InvalidDataException($"Unsupported TAF change marker '{marker}'.");

            List<string> conditionTokens = [];
            while (index < tokens.Length && !IsChangeStart(tokens[index])) conditionTokens.Add(tokens[index++]);
            changes.Add(new TafForecastGroup(kind, period, from, probability, ParseConditions(conditionTokens)));
        }

        return new DecodedTaf(normalized, station, issueTime, validity, isAmended, isCorrected, false, baseForecast, changes);
    }

    private static TafForecastGroup CreateEmptyBase(TafPeriod validity) => new(TafChangeKind.Base, validity, null, null, new TafConditions(null, null, false, [], []));
    private static bool ConsumeToken(string[] tokens, ref int index, string value) { if (index >= tokens.Length || !tokens[index].Equals(value, StringComparison.OrdinalIgnoreCase)) return false; index++; return true; }
    private static bool IsChangeStart(string token) => token.StartsWith("FM", StringComparison.OrdinalIgnoreCase) || token.Equals("BECMG", StringComparison.OrdinalIgnoreCase) || token.Equals("TEMPO", StringComparison.OrdinalIgnoreCase) || token.Equals("PROB30", StringComparison.OrdinalIgnoreCase) || token.Equals("PROB40", StringComparison.OrdinalIgnoreCase);
    private static TafPeriod RequirePeriod(string[] tokens, ref int index, string marker) => index < tokens.Length && PeriodRegex().IsMatch(tokens[index]) ? ParsePeriod(tokens[index++]) : throw new InvalidDataException($"TAF group '{marker}' requires a validity period.");
    private static TafTimePoint ParseIssueTime(string token) { Match match = IssueRegex().Match(token); return match.Success ? new TafTimePoint(ParseNumber(match.Groups["day"].Value), ParseNumber(match.Groups["hour"].Value), ParseNumber(match.Groups["minute"].Value)) : throw new InvalidDataException("The TAF issue time is invalid."); }
    private static TafPeriod ParsePeriod(string token) { Match match = PeriodRegex().Match(token); if (!match.Success) throw new InvalidDataException($"Invalid TAF period '{token}'."); return new TafPeriod(new TafTimePoint(ParseNumber(match.Groups["startDay"].Value), ParseHour(match.Groups["startHour"].Value)), new TafTimePoint(ParseNumber(match.Groups["endDay"].Value), ParseHour(match.Groups["endHour"].Value))); }
    private static TafTimePoint ParseFrom(string marker) { Match match = FromRegex().Match(marker); return match.Success ? new TafTimePoint(ParseNumber(match.Groups["day"].Value), ParseNumber(match.Groups["hour"].Value), ParseNumber(match.Groups["minute"].Value)) : throw new InvalidDataException($"Invalid TAF FM group '{marker}'."); }
    private static int ParseHour(string value) => value == "24" ? 0 : ParseNumber(value);

    private static TafConditions ParseConditions(IEnumerable<string> tokens)
    {
        MetarWind? wind = null;
        int? visibility = null;
        bool isCavok = false;
        List<string> weather = [];
        List<MetarCloudLayer> clouds = [];

        foreach (string rawToken in tokens)
        {
            string token = rawToken.ToUpperInvariant();
            if (token is "NSW") { weather.Clear(); continue; }
            if (token is "CAVOK") { isCavok = true; visibility = 10_000; continue; }
            if (TryParseWind(token, out MetarWind? parsedWind) && parsedWind is not null) { wind = parsedWind; continue; }
            if (TryParseVisibility(token, out int parsedVisibility)) { visibility = parsedVisibility; continue; }
            if (TryParseCloud(token, out MetarCloudLayer? cloud) && cloud is not null) { clouds.Add(cloud); continue; }
            if (WeatherRegex().IsMatch(token)) weather.Add(token);
        }

        return new TafConditions(wind, visibility, isCavok, weather, clouds);
    }

    private static bool TryParseWind(string token, out MetarWind? wind)
    {
        Match match = WindRegex().Match(token);
        if (!match.Success) { wind = null; return false; }
        bool variable = match.Groups["direction"].Value == "VRB";
        int speed = ParseNumber(match.Groups["speed"].Value);
        int? gust = match.Groups["gust"].Success ? ParseNumber(match.Groups["gust"].Value) : null;
        if (match.Groups["unit"].Value == "MPS") { speed = ToKnots(speed); gust = gust is null ? null : ToKnots(gust.Value); }
        wind = new MetarWind(variable ? null : ParseNumber(match.Groups["direction"].Value), speed, gust, variable);
        return true;
    }

    private static bool TryParseVisibility(string token, out int visibility)
    {
        if (MetricVisibilityRegex().IsMatch(token)) { visibility = token == "9999" ? 10_000 : ParseNumber(token); return true; }
        Match statuteMatch = StatuteVisibilityRegex().Match(token);
        if (!statuteMatch.Success) { visibility = default; return false; }
        double miles = ParseStatuteMiles(statuteMatch.Groups["value"].Value);
        visibility = (int)Math.Round(miles * 1609.344, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryParseCloud(string token, out MetarCloudLayer? cloud)
    {
        if (token is "NSC" or "NCD" or "SKC" or "CLR") { cloud = new MetarCloudLayer(token, null, null); return true; }
        Match match = CloudRegex().Match(token);
        if (!match.Success) { cloud = null; return false; }
        int? height = match.Groups["height"].Value == "///" ? null : ParseNumber(match.Groups["height"].Value) * 100;
        cloud = new MetarCloudLayer(match.Groups["amount"].Value, height, match.Groups["type"].Success ? match.Groups["type"].Value : null);
        return true;
    }

    private static double ParseStatuteMiles(string value)
    {
        string normalized = value.TrimStart('P', 'M');
        if (!normalized.Contains('/')) return double.Parse(normalized, CultureInfo.InvariantCulture);
        string[] parts = normalized.Split('/');
        return double.Parse(parts[0], CultureInfo.InvariantCulture) / double.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    private static int ParseNumber(string value) => int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    private static int ToKnots(int value) => (int)Math.Round(value * 1.943844, MidpointRounding.AwayFromZero);

    [GeneratedRegex("^[A-Z]{4}$", RegexOptions.CultureInvariant)] private static partial Regex StationRegex();
    [GeneratedRegex("^(?<day>\\d{2})(?<hour>\\d{2})(?<minute>\\d{2})Z$", RegexOptions.CultureInvariant)] private static partial Regex IssueRegex();
    [GeneratedRegex("^(?<startDay>\\d{2})(?<startHour>\\d{2})/(?<endDay>\\d{2})(?<endHour>\\d{2})$", RegexOptions.CultureInvariant)] private static partial Regex PeriodRegex();
    [GeneratedRegex("^FM(?<day>\\d{2})(?<hour>\\d{2})(?<minute>\\d{2})$", RegexOptions.CultureInvariant)] private static partial Regex FromRegex();
    [GeneratedRegex("^(?<direction>\\d{3}|VRB)(?<speed>\\d{2,3})(?:G(?<gust>\\d{2,3}))?(?<unit>KT|MPS)$", RegexOptions.CultureInvariant)] private static partial Regex WindRegex();
    [GeneratedRegex("^\\d{4}$", RegexOptions.CultureInvariant)] private static partial Regex MetricVisibilityRegex();
    [GeneratedRegex("^(?<value>[PM]?(?:\\d+|\\d/\\d))SM$", RegexOptions.CultureInvariant)] private static partial Regex StatuteVisibilityRegex();
    [GeneratedRegex("^(?<amount>FEW|SCT|BKN|OVC|VV)(?<height>\\d{3}|///)(?<type>CB|TCU)?$", RegexOptions.CultureInvariant)] private static partial Regex CloudRegex();
    [GeneratedRegex("^[+-]?(?:VC)?(?:MI|PR|BC|DR|BL|SH|TS|FZ)?(?:DZ|RA|SN|SG|IC|PL|GR|GS|UP|BR|FG|FU|VA|DU|SA|HZ|PY|PO|SQ|FC|SS|DS)+$", RegexOptions.CultureInvariant)] private static partial Regex WeatherRegex();
}
