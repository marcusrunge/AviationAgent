using System.Globalization;

namespace MarcusRunge.AviationAgent.Core;

public sealed class AviationWeatherReportFormatter : IAviationWeatherReportFormatter
{
    public FormattedWeatherReport Format(AviationWeatherReport report, AgentFocus focus, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(culture);
        string formattedText = report.Product switch
        {
            WeatherProduct.Metar => MetarFormatter.Format(MetarParser.Parse(report.RawText), focus, culture),
            WeatherProduct.Taf => TafFormatter.Format(TafParser.Parse(report.RawText), focus, culture),
            _ => throw new ArgumentOutOfRangeException(nameof(report), report.Product, "The weather product cannot be formatted."),
        };
        return new FormattedWeatherReport(report.Product, report.Station, formattedText, report.RawText, report.RetrievedAt);
    }
}
