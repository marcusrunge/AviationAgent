namespace MarcusRunge.AviationAgent.Core;

/// <summary>Contains the deterministic subset decoded from a raw METAR report.</summary>
public sealed record DecodedMetar(
    string RawText,
    string Station,
    int ObservationDay,
    TimeOnly ObservationTimeUtc,
    MetarWind? Wind,
    int? VisibilityMeters,
    bool IsCavok,
    IReadOnlyList<string> WeatherPhenomena,
    IReadOnlyList<MetarCloudLayer> CloudLayers,
    int? TemperatureCelsius,
    int? DewPointCelsius,
    int? QnhHectopascals);
