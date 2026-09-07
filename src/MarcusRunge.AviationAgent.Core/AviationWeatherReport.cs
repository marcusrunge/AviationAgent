namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Represents one raw aviation weather report returned by a deterministic data source.
/// </summary>
public sealed record AviationWeatherReport(
    WeatherProduct Product,
    string Station,
    string RawText,
    DateTimeOffset RetrievedAt);
