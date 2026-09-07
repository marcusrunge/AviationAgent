namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Retrieves raw aviation weather reports from a deterministic source.
/// </summary>
public interface IAviationWeatherClient
{
    Task<AviationWeatherReport> GetMetarAsync(string station, CancellationToken cancellationToken);

    Task<AviationWeatherReport> GetTafAsync(string station, CancellationToken cancellationToken);
}
