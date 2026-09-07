namespace MarcusRunge.AviationAgent.AviationWeather;

/// <summary>
/// Configures access to the Aviation Weather Center Data API.
/// </summary>
public sealed class AviationWeatherClientOptions
{
    public Uri BaseAddress { get; init; } = new("https://aviationweather.gov/");

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
