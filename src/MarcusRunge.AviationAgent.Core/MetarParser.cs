using System.Globalization;
using System.Text.RegularExpressions;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Decodes a conservative, deterministic subset of international METAR syntax.
/// </summary>
public static partial class MetarParser
{
    private static readonly HashSet<string> IgnoredTokens =
    [
        "AUTO",
        "COR",
        "NIL",
        "NOSIG",
    ];

    /// <summary>
    /// Parses a raw METAR report into a structured representation.
    /// </summary>
    /// <param name="rawText">The raw METAR report.</param>
    /// <returns>The deterministically decoded METAR data.</returns>
    /// <exception cref="ArgumentException">
    /// The supplied report is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// The report does not contain the required METAR fields.
    /// </exception>
    public static DecodedMetar Parse(string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);

        string normalized = rawText
            .Trim()
            .TrimEnd('=')
            .Trim();

        string[] tokens = normalized.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        int index = tokens.Length > 0
            && tokens[0] is "METAR" or "SPECI"
                ? 1
                : 0;

        if (tokens.Length < index + 2)
        {
            throw new InvalidDataException(
                "The METAR does not contain a station and observation time.");
        }

        string station = tokens[index++]
            .ToUpperInvariant();

        if (!StationRegex().IsMatch(station))
        {
            throw new InvalidDataException(
                $"Invalid METAR station '{station}'.");
        }

        Match observationMatch =
            ObservationTimeRegex().Match(tokens[index++]);

        if (!observationMatch.Success)
        {
            throw new InvalidDataException(
                "The METAR observation time is invalid.");
        }

        int day = ParseNumber(
            observationMatch.Groups["day"].Value);

        TimeOnly time = new(
            ParseNumber(
                observationMatch.Groups["hour"].Value),
            ParseNumber(
                observationMatch.Groups["minute"].Value));

        MetarWind? wind = null;
        int? visibility = null;
        bool isCavok = false;
        List<string> weather = [];
        List<MetarCloudLayer> clouds = [];
        int? temperature = null;
        int? dewPoint = null;
        int? qnh = null;

        for (; index < tokens.Length; index++)
        {
            string token = tokens[index]
                .ToUpperInvariant();

            // Remarks are intentionally not decoded. They can contain
            // national and station-specific syntax outside the supported
            // deterministic METAR subset.
            if (token == "RMK")
            {
                break;
            }

            if (IgnoredTokens.Contains(token))
            {
                continue;
            }

            if (TryParseWind(
                token,
                out MetarWind? parsedWind))
            {
                wind = parsedWind;
                continue;
            }

            if (token == "CAVOK")
            {
                isCavok = true;
                visibility = 10_000;
                continue;
            }

            if (VisibilityRegex().IsMatch(token))
            {
                visibility = token == "9999"
                    ? 10_000
                    : ParseNumber(token);

                continue;
            }

            // The explicit null check communicates the Try-pattern invariant
            // to nullable flow analysis and prevents a nullable value from
            // being inserted into the cloud layer collection.
            if (TryParseCloud(
                    token,
                    out MetarCloudLayer? parsedCloud)
                && parsedCloud is not null)
            {
                clouds.Add(parsedCloud);
                continue;
            }

            if (TryParseTemperature(
                token,
                out int? parsedTemperature,
                out int? parsedDewPoint))
            {
                temperature = parsedTemperature;
                dewPoint = parsedDewPoint;
                continue;
            }

            if (TryParseQnh(
                token,
                out int parsedQnh))
            {
                qnh = parsedQnh;
                continue;
            }

            if (WeatherRegex().IsMatch(token))
            {
                weather.Add(token);
            }
        }

        return new DecodedMetar(
            normalized,
            station,
            day,
            time,
            wind,
            visibility,
            isCavok,
            weather,
            clouds,
            temperature,
            dewPoint,
            qnh);
    }

    private static bool TryParseWind(
        string token,
        out MetarWind? wind)
    {
        Match match = WindRegex().Match(token);

        if (!match.Success)
        {
            wind = null;
            return false;
        }

        bool isVariable =
            match.Groups["direction"].Value == "VRB";

        int? direction = isVariable
            ? null
            : ParseNumber(
                match.Groups["direction"].Value);

        int speed = ParseNumber(
            match.Groups["speed"].Value);

        int? gust = match.Groups["gust"].Success
            ? ParseNumber(
                match.Groups["gust"].Value)
            : null;

        if (match.Groups["unit"].Value == "MPS")
        {
            speed = ConvertMetersPerSecondToKnots(
                speed);

            gust = gust is null
                ? null
                : ConvertMetersPerSecondToKnots(
                    gust.Value);
        }

        wind = new MetarWind(
            direction,
            speed,
            gust,
            isVariable);

        return true;
    }

    private static bool TryParseCloud(
        string token,
        out MetarCloudLayer? cloud)
    {
        if (token is "NSC"
            or "NCD"
            or "SKC"
            or "CLR")
        {
            cloud = new MetarCloudLayer(
                token,
                null,
                null);

            return true;
        }

        Match match = CloudRegex().Match(token);

        if (!match.Success)
        {
            cloud = null;
            return false;
        }

        int? height =
            match.Groups["height"].Value == "///"
                ? null
                : ParseNumber(
                    match.Groups["height"].Value) * 100;

        string? cloudType =
            match.Groups["type"].Success
                ? match.Groups["type"].Value
                : null;

        cloud = new MetarCloudLayer(
            match.Groups["amount"].Value,
            height,
            cloudType);

        return true;
    }

    private static bool TryParseTemperature(
        string token,
        out int? temperature,
        out int? dewPoint)
    {
        Match match =
            TemperatureRegex().Match(token);

        if (!match.Success)
        {
            temperature = null;
            dewPoint = null;
            return false;
        }

        temperature = ParseSignedTemperature(
            match.Groups["temperature"].Value);

        dewPoint =
            match.Groups["dewpoint"].Value == "//"
                ? null
                : ParseSignedTemperature(
                    match.Groups["dewpoint"].Value);

        return true;
    }

    private static bool TryParseQnh(
        string token,
        out int qnh)
    {
        Match match = QnhRegex().Match(token);

        qnh = match.Success
            ? ParseNumber(
                match.Groups["qnh"].Value)
            : default;

        return match.Success;
    }

    private static int ParseSignedTemperature(
        string value) =>
        value.StartsWith('M')
            ? -ParseNumber(value[1..])
            : ParseNumber(value);

    private static int ParseNumber(
        string value) =>
        int.Parse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture);

    private static int ConvertMetersPerSecondToKnots(
        int value) =>
        (int)Math.Round(
            value * 1.943844,
            MidpointRounding.AwayFromZero);

    [GeneratedRegex(
        "^[A-Z]{4}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StationRegex();

    [GeneratedRegex(
        "^(?<day>\\d{2})(?<hour>\\d{2})(?<minute>\\d{2})Z$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ObservationTimeRegex();

    [GeneratedRegex(
        "^(?<direction>\\d{3}|VRB)(?<speed>\\d{2,3})(?:G(?<gust>\\d{2,3}))?(?<unit>KT|MPS)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WindRegex();

    [GeneratedRegex(
        "^\\d{4}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex VisibilityRegex();

    [GeneratedRegex(
        "^(?<amount>FEW|SCT|BKN|OVC|VV)(?<height>\\d{3}|///)(?<type>CB|TCU)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CloudRegex();

    [GeneratedRegex(
        "^(?<temperature>M?\\d{2})/(?<dewpoint>M?\\d{2}|//)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TemperatureRegex();

    [GeneratedRegex(
        "^Q(?<qnh>\\d{4})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex QnhRegex();

    [GeneratedRegex(
        "^[+-]?(?:VC)?(?:MI|PR|BC|DR|BL|SH|TS|FZ)?(?:DZ|RA|SN|SG|IC|PL|GR|GS|UP|BR|FG|FU|VA|DU|SA|HZ|PY|PO|SQ|FC|SS|DS)+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WeatherRegex();
}