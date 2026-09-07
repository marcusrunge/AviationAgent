namespace MarcusRunge.AviationAgent.Core;

public static class AgentWireValues
{
    public static string ToWireValue(this AgentAction value) => value switch
    {
        AgentAction.GetMetar => "get_metar",
        AgentAction.GetTaf => "get_taf",
        AgentAction.GetMetarAndTaf => "get_metar_and_taf",
        AgentAction.ExplainPreviousResult => "explain_previous_result",
        AgentAction.FilterPreviousResult => "filter_previous_result",
        AgentAction.UnsupportedOperationalDecision => "unsupported_operational_decision",
        AgentAction.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToWireValue(this AgentFocus value) => value switch
    {
        AgentFocus.Full => "full", AgentFocus.Wind => "wind", AgentFocus.Visibility => "visibility",
        AgentFocus.Clouds => "clouds", AgentFocus.Weather => "weather", AgentFocus.Temperature => "temperature",
        AgentFocus.Pressure => "pressure", AgentFocus.Validity => "validity", AgentFocus.Changes => "changes",
        AgentFocus.WorstConditions => "worst_conditions", AgentFocus.None => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
