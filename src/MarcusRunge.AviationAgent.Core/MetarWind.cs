namespace MarcusRunge.AviationAgent.Core;

/// <summary>Represents the surface wind group of a METAR report.</summary>
public sealed record MetarWind(int? DirectionDegrees, int SpeedKnots, int? GustKnots, bool IsVariable);
