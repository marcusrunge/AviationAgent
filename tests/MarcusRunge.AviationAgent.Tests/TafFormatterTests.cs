using System.Globalization;
using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class TafFormatterTests
{
    private const string Raw = "TAF EDDV 121100Z 1212/1312 25012KT 9999 FEW040 TEMPO 1216/1219 25018G28KT 6000 SHRA BKN025CB BECMG 1306/1308 24010KT 9999 SCT040";

    [Fact]
    public void Format_FullGerman_ReturnsHeaderAndTimeline()
    {
        string result = TafFormatter.Format(TafParser.Parse(Raw), AgentFocus.Full, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Contains("TAF EDDV", result);
        Assert.Contains("Ausgangslage", result);
        Assert.Contains("Zeitweise", result);
        Assert.Contains("Übergang", result);
    }

    [Fact]
    public void Format_WindFocus_ContainsOnlyGroupsWithWind()
    {
        string result = TafFormatter.Format(TafParser.Parse(Raw), AgentFocus.Wind, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Contains("250°", result);
        Assert.Contains("Böen bis 28 kt", result);
    }

    [Fact]
    public void ReportFormatter_Taf_UsesDeterministicTafFormatter()
    {
        AviationWeatherReport report = new(WeatherProduct.Taf, "EDDV", Raw, DateTimeOffset.UnixEpoch);
        FormattedWeatherReport result = new AviationWeatherReportFormatter().Format(report, AgentFocus.Validity, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Contains("Gültigkeit", result.FormattedText);
        Assert.Equal(Raw, result.RawText);
    }
}
