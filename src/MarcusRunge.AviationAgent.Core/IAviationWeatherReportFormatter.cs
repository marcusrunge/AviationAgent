using System.Globalization;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Formats a raw aviation weather report according to the requested focus.
/// </summary>
public interface IAviationWeatherReportFormatter
{
    FormattedWeatherReport Format(AviationWeatherReport report, AgentFocus focus, CultureInfo culture);
}
