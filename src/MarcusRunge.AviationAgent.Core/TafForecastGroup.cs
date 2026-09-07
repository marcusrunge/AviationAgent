namespace MarcusRunge.AviationAgent.Core;

public sealed record TafForecastGroup(TafChangeKind Kind, TafPeriod? Period, TafTimePoint? From, int? ProbabilityPercent, TafConditions Conditions);
