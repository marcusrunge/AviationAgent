using System.Net;
using System.Text.RegularExpressions;
using MarcusRunge.AviationAgent.Core;

namespace MarcusRunge.AviationAgent.AviationWeather;

/// <summary>
/// Retrieves current raw METAR and TAF reports from the Aviation Weather Center Data API.
/// </summary>
public sealed partial class AviationWeatherGovClient : IAviationWeatherClient
{
    private const string UserAgent = "MarcusRunge-AviationAgent/1.0";
    private readonly HttpClient _httpClient;
    private readonly AviationWeatherClientOptions _options;

    public AviationWeatherGovClient(HttpClient httpClient, AviationWeatherClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.BaseAddress.IsAbsoluteUri) throw new ArgumentException("BaseAddress must be absolute.", nameof(options));
        if (options.RequestTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), options.RequestTimeout, "RequestTimeout must be greater than zero.");

        _httpClient = httpClient;
        _options = options;
    }

    public Task<AviationWeatherReport> GetMetarAsync(string station, CancellationToken cancellationToken) =>
        GetReportAsync(WeatherProduct.Metar, "api/data/metar", station, cancellationToken);

    public Task<AviationWeatherReport> GetTafAsync(string station, CancellationToken cancellationToken) =>
        GetReportAsync(WeatherProduct.Taf, "api/data/taf", station, cancellationToken);

    private async Task<AviationWeatherReport> GetReportAsync(WeatherProduct product, string endpoint, string station, CancellationToken cancellationToken)
    {
        string normalizedStation = NormalizeStation(station);
        Uri requestUri = new(_options.BaseAddress, $"{endpoint}?ids={Uri.EscapeDataString(normalizedStation)}&format=raw");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.RequestTimeout);

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent) throw new AviationWeatherClientException($"No current {product} report is available for station {normalizedStation}.", response.StatusCode);
            if (!response.IsSuccessStatusCode) throw new AviationWeatherClientException($"The aviation weather service returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).", response.StatusCode);

            string rawText = (await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false)).Trim();
            if (string.IsNullOrWhiteSpace(rawText)) throw new AviationWeatherClientException($"The aviation weather service returned an empty {product} report for station {normalizedStation}.", response.StatusCode);

            return new AviationWeatherReport(product, normalizedStation, rawText, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AviationWeatherClientException($"The aviation weather request for station {normalizedStation} timed out after {_options.RequestTimeout}.", null, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AviationWeatherClientException($"The aviation weather request for station {normalizedStation} failed.", exception.StatusCode, exception);
        }
    }

    private static string NormalizeStation(string station)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(station);
        string normalizedStation = station.Trim().ToUpperInvariant();
        return IcaoStationRegex().IsMatch(normalizedStation) ? normalizedStation : throw new ArgumentException($"'{station}' is not a valid four-letter ICAO station identifier.", nameof(station));
    }

    [GeneratedRegex("^[A-Z]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex IcaoStationRegex();
}
