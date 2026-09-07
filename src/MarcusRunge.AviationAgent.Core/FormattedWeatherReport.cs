namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Contains a user-facing deterministic representation and the unchanged source report.
/// </summary>
public sealed record FormattedWeatherReport(WeatherProduct Product, string Station, string FormattedText, string RawText, DateTimeOffset RetrievedAt);
