namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Routes a validated agent decision to deterministic aviation weather operations.
/// </summary>
public sealed class AgentDecisionRouter(IAviationWeatherClient weatherClient)
{
    private readonly IAviationWeatherClient _weatherClient = weatherClient ?? throw new ArgumentNullException(nameof(weatherClient));

    public async Task<AgentRouteResult> RouteAsync(AgentDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AviationWeatherReport> reports = decision.Action switch
        {
            AgentAction.GetMetar => [await GetMetarAsync(decision, cancellationToken).ConfigureAwait(false)],
            AgentAction.GetTaf => [await GetTafAsync(decision, cancellationToken).ConfigureAwait(false)],
            AgentAction.GetMetarAndTaf => await GetMetarAndTafAsync(decision, cancellationToken).ConfigureAwait(false),
            AgentAction.Unknown or AgentAction.UnsupportedOperationalDecision or AgentAction.ExplainPreviousResult or AgentAction.FilterPreviousResult => [],
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision.Action, "The agent action cannot be routed."),
        };

        return new AgentRouteResult(decision, reports);
    }

    private Task<AviationWeatherReport> GetMetarAsync(AgentDecision decision, CancellationToken cancellationToken) =>
        _weatherClient.GetMetarAsync(RequireStation(decision), cancellationToken);

    private Task<AviationWeatherReport> GetTafAsync(AgentDecision decision, CancellationToken cancellationToken) =>
        _weatherClient.GetTafAsync(RequireStation(decision), cancellationToken);

    private async Task<IReadOnlyList<AviationWeatherReport>> GetMetarAndTafAsync(AgentDecision decision, CancellationToken cancellationToken)
    {
        string station = RequireStation(decision);

        // The two independent network operations start before either is awaited,
        // minimizing latency while preserving the stable METAR-then-TAF result order.
        Task<AviationWeatherReport> metarTask = _weatherClient.GetMetarAsync(station, cancellationToken);
        Task<AviationWeatherReport> tafTask = _weatherClient.GetTafAsync(station, cancellationToken);
        await Task.WhenAll(metarTask, tafTask).ConfigureAwait(false);

        return [await metarTask.ConfigureAwait(false), await tafTask.ConfigureAwait(false)];
    }

    private static string RequireStation(AgentDecision decision) =>
        !string.IsNullOrWhiteSpace(decision.Station)
            ? decision.Station
            : throw new InvalidDataException($"Action '{decision.Action.ToWireValue()}' requires an ICAO station.");
}
