using System.Globalization;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Selects the deterministic formatter for a retrieved aviation weather product.
/// </summary>
public sealed class AviationWeatherReportFormatter : IAviationWeatherReportFormatter
{
    public FormattedWeatherReport Format(AviationWeatherReport report, AgentFocus focus, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(culture);

        // METAR is decoded deterministically. TAF deliberately remains raw
        // until its own parser can preserve change groups and validity periods.
        string formattedText = report.Product switch
        {
            WeatherProduct.Metar => MetarFormatter.Format(MetarParser.Parse(report.RawText), focus, culture),
            WeatherProduct.Taf => report.RawText,
            _ => throw new ArgumentOutOfRangeException(nameof(report), report.Product, "The weather product cannot be formatted."),
        };

        return new FormattedWeatherReport(report.Product, report.Station, formattedText, report.RawText, report.RetrievedAt);
    }
}
