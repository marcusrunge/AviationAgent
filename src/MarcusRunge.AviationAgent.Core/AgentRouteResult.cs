namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Contains the validated agent decision and any reports retrieved for that decision.
/// </summary>
public sealed record AgentRouteResult(AgentDecision Decision, IReadOnlyList<AviationWeatherReport> Reports)
{
    public bool HasReports => Reports.Count > 0;
}
