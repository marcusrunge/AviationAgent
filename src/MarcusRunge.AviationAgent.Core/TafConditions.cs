namespace MarcusRunge.AviationAgent.Core;

public sealed record TafConditions(MetarWind? Wind, int? VisibilityMeters, bool IsCavok, IReadOnlyList<string> WeatherPhenomena, IReadOnlyList<MetarCloudLayer> CloudLayers);
