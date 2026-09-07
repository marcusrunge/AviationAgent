namespace MarcusRunge.AviationAgent.Core;

/// <summary>Represents one cloud layer reported by a METAR.</summary>
public sealed record MetarCloudLayer(string Amount, int? BaseFeetAboveAerodrome, string? CloudType);
