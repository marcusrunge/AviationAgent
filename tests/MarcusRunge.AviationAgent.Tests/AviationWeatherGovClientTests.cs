using System.Net;
using MarcusRunge.AviationAgent.AviationWeather;
using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class AviationWeatherGovClientTests
{
    [Fact]
    public async Task GetMetarAsync_ValidResponse_ReturnsNormalizedRawReport()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingHandler handler = new(HttpStatusCode.OK, "  METAR EDDV 070750Z  ");
        AviationWeatherGovClient client = CreateClient(handler);

        AviationWeatherReport report = await client.GetMetarAsync("eddv", cancellationToken);

        Assert.Equal(WeatherProduct.Metar, report.Product);
        Assert.Equal("EDDV", report.Station);
        Assert.Equal("METAR EDDV 070750Z", report.RawText);
        Assert.Equal("https://aviationweather.gov/api/data/metar?ids=EDDV&format=raw", handler.RequestUri?.AbsoluteUri);
        Assert.Equal("MarcusRunge-AviationAgent/1.0", handler.UserAgent);
    }

    [Fact]
    public async Task GetTafAsync_ValidResponse_UsesTafEndpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingHandler handler = new(HttpStatusCode.OK, "TAF ETHS 070500Z 0706/0806 CAVOK");
        AviationWeatherGovClient client = CreateClient(handler);

        AviationWeatherReport report = await client.GetTafAsync("ETHS", cancellationToken);

        Assert.Equal(WeatherProduct.Taf, report.Product);
        Assert.Equal("https://aviationweather.gov/api/data/taf?ids=ETHS&format=raw", handler.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetMetarAsync_NoContent_ThrowsDomainException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        AviationWeatherGovClient client = CreateClient(new RecordingHandler(HttpStatusCode.NoContent, string.Empty));

        AviationWeatherClientException exception = await Assert.ThrowsAsync<AviationWeatherClientException>(() => client.GetMetarAsync("EDDV", cancellationToken));

        Assert.Equal(HttpStatusCode.NoContent, exception.StatusCode);
    }

    [Fact]
    public async Task GetTafAsync_RateLimited_ThrowsDomainException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        AviationWeatherGovClient client = CreateClient(new RecordingHandler(HttpStatusCode.TooManyRequests, "rate limited"));

        AviationWeatherClientException exception = await Assert.ThrowsAsync<AviationWeatherClientException>(() => client.GetTafAsync("ETHS", cancellationToken));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public async Task GetMetarAsync_InvalidStation_DoesNotSendRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingHandler handler = new(HttpStatusCode.OK, "unused");
        AviationWeatherGovClient client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetMetarAsync("ED1V", cancellationToken));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetMetarAsync_CancelledToken_PropagatesCancellation()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        AviationWeatherGovClient client = CreateClient(new RecordingHandler(HttpStatusCode.OK, "unused"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetMetarAsync("EDDV", cancellationTokenSource.Token));
    }

    private static AviationWeatherGovClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new AviationWeatherClientOptions());

    private sealed class RecordingHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? UserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            RequestUri = request.RequestUri;
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        }
    }
}
