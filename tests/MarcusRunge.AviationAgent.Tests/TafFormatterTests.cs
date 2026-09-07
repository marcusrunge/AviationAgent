using System.Globalization;
using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class TafFormatterTests
{
    private const string RawTaf = "TAF EDDV 121100Z 1212/1312 25012KT 9999 FEW040 TEMPO 1216/1219 25018G28KT 6000 SHRA BKN025CB BECMG 1306/1308 24010KT 9999 SCT040";

    [Fact]
    public void Format_FullGerman_ReturnsLocalizedCloudAmounts()
    {
        string result = TafFormatter.Format(TafParser.Parse(RawTaf), AgentFocus.Full, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("gering bewölkt in 4.000 ft AGL", result);
        Assert.Contains("durchbrochen in 2.500 ft AGL, Typ CB", result);
        Assert.Contains("aufgelockert in 4.000 ft AGL", result);
        Assert.DoesNotContain("FEW in", result);
        Assert.DoesNotContain("BKN in", result);
        Assert.DoesNotContain("SCT in", result);
    }

    [Fact]
    public void Format_FullEnglish_ReturnsLocalizedCloudAmounts()
    {
        string result = TafFormatter.Format(TafParser.Parse(RawTaf), AgentFocus.Full, CultureInfo.GetCultureInfo("en-US"));

        Assert.Contains("few at 4,000 ft AGL", result);
        Assert.Contains("broken at 2,500 ft AGL, type CB", result);
        Assert.Contains("scattered at 4,000 ft AGL", result);
    }

    [Fact]
    public void Format_WindFocus_ContainsOnlyWindInformation()
    {
        string result = TafFormatter.Format(TafParser.Parse(RawTaf), AgentFocus.Wind, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("aus 250° mit 12 kt", result);
        Assert.Contains("Böen bis 28 kt", result);
        Assert.DoesNotContain("bewölkt", result);
    }

    [Fact]
    public void Format_CloudFocus_ContainsOnlyLocalizedCloudInformation()
    {
        string result = TafFormatter.Format(TafParser.Parse(RawTaf), AgentFocus.Clouds, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("gering bewölkt in 4.000 ft AGL", result);
        Assert.Contains("durchbrochen in 2.500 ft AGL, Typ CB", result);
        Assert.Contains("aufgelockert in 4.000 ft AGL", result);
        Assert.DoesNotContain("250°", result);
    }

    [Fact]
    public void Format_WeatherFocus_PreservesOfficialWeatherCode()
    {
        string result = TafFormatter.Format(TafParser.Parse(RawTaf), AgentFocus.Weather, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("SHRA", result);
        Assert.DoesNotContain("bewölkt", result);
    }

    [Fact]
    public void ReportFormatter_Taf_PreservesRawReport()
    {
        AviationWeatherReport report = new(WeatherProduct.Taf, "EDDV", RawTaf, DateTimeOffset.UnixEpoch);
        FormattedWeatherReport result = new AviationWeatherReportFormatter().Format(report, AgentFocus.Validity, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("Gültigkeit", result.FormattedText);
        Assert.Equal(RawTaf, result.RawText);
    }
}
