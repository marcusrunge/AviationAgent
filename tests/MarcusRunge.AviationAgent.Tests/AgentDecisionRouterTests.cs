using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class AgentDecisionRouterTests
{
    [Fact]
    public async Task RouteAsync_GetMetar_RetrievesOnlyMetar()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FakeAviationWeatherClient client = new();
        AgentDecisionRouter router = new(client);
        AgentDecision decision = new(AgentAction.GetMetar, "ETHS", AgentFocus.Full);

        AgentRouteResult result = await router.RouteAsync(decision, cancellationToken);

        AviationWeatherReport report = Assert.Single(result.Reports);
        Assert.Equal(WeatherProduct.Metar, report.Product);
        Assert.Equal(["METAR:ETHS"], client.Calls);
    }

    [Fact]
    public async Task RouteAsync_GetTaf_RetrievesOnlyTaf()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FakeAviationWeatherClient client = new();
        AgentDecisionRouter router = new(client);
        AgentDecision decision = new(AgentAction.GetTaf, "EDDH", AgentFocus.Wind);

        AgentRouteResult result = await router.RouteAsync(decision, cancellationToken);

        AviationWeatherReport report = Assert.Single(result.Reports);
        Assert.Equal(WeatherProduct.Taf, report.Product);
        Assert.Equal(["TAF:EDDH"], client.Calls);
    }

    [Fact]
    public async Task RouteAsync_GetMetarAndTaf_RetrievesBothInStableOrder()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FakeAviationWeatherClient client = new();
        AgentDecisionRouter router = new(client);
        AgentDecision decision = new(AgentAction.GetMetarAndTaf, "EDDF", AgentFocus.Full);

        AgentRouteResult result = await router.RouteAsync(decision, cancellationToken);

        Assert.Collection(
            result.Reports,
            report => Assert.Equal(WeatherProduct.Metar, report.Product),
            report => Assert.Equal(WeatherProduct.Taf, report.Product));
        Assert.Equal(2, client.Calls.Count);
        Assert.Contains("METAR:EDDF", client.Calls);
        Assert.Contains("TAF:EDDF", client.Calls);
    }

    [Theory]
    [InlineData(AgentAction.Unknown)]
    [InlineData(AgentAction.UnsupportedOperationalDecision)]
    [InlineData(AgentAction.ExplainPreviousResult)]
    [InlineData(AgentAction.FilterPreviousResult)]
    public async Task RouteAsync_NonWeatherAction_DoesNotCallWeatherClient(AgentAction action)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FakeAviationWeatherClient client = new();
        AgentDecisionRouter router = new(client);
        AgentDecision decision = new(action, null, AgentFocus.None);

        AgentRouteResult result = await router.RouteAsync(decision, cancellationToken);

        Assert.Empty(result.Reports);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task RouteAsync_WeatherActionWithoutStation_Throws()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        AgentDecisionRouter router = new(new FakeAviationWeatherClient());
        AgentDecision decision = new(AgentAction.GetMetar, null, AgentFocus.Full);

        await Assert.ThrowsAsync<InvalidDataException>(() => router.RouteAsync(decision, cancellationToken));
    }

    [Fact]
    public async Task RouteAsync_CancelledToken_DoesNotCallWeatherClient()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        FakeAviationWeatherClient client = new();
        AgentDecisionRouter router = new(client);
        AgentDecision decision = new(AgentAction.GetMetar, "ETHS", AgentFocus.Full);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => router.RouteAsync(decision, cancellationTokenSource.Token));
        Assert.Empty(client.Calls);
    }

    private sealed class FakeAviationWeatherClient : IAviationWeatherClient
    {
        public List<string> Calls { get; } = [];

        public Task<AviationWeatherReport> GetMetarAsync(string station, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add($"METAR:{station}");
            return Task.FromResult(new AviationWeatherReport(WeatherProduct.Metar, station, $"METAR {station} TEST", DateTimeOffset.UnixEpoch));
        }

        public Task<AviationWeatherReport> GetTafAsync(string station, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add($"TAF:{station}");
            return Task.FromResult(new AviationWeatherReport(WeatherProduct.Taf, station, $"TAF {station} TEST", DateTimeOffset.UnixEpoch));
        }
    }
}
