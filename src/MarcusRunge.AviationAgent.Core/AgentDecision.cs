namespace MarcusRunge.AviationAgent.Core;

public sealed record AgentDecision(AgentAction Action, string? Station, AgentFocus Focus);
