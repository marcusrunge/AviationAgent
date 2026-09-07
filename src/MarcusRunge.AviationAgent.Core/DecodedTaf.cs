namespace MarcusRunge.AviationAgent.Core;

public sealed record DecodedTaf(string RawText, string Station, TafTimePoint IssueTime, TafPeriod Validity, bool IsAmended, bool IsCorrected, bool IsCancelled, TafForecastGroup BaseForecast, IReadOnlyList<TafForecastGroup> ChangeGroups);
