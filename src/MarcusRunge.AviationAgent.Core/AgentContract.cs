namespace MarcusRunge.AviationAgent.Core;

public static class AgentContract
{
    public static HashSet<string> Actions { get; } = ["get_metar", "get_taf", "get_metar_and_taf", "explain_previous_result", "filter_previous_result", "unsupported_operational_decision", "unknown"];
    public static HashSet<string> Focuses { get; } = ["full", "wind", "visibility", "clouds", "weather", "temperature", "pressure", "validity", "changes", "worst_conditions", "none"];
}
