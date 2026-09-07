using System.Globalization;
using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class AviationWeatherReportFormatterTests
{
    [Fact]
    public void Format_MetarWeatherFocus_ReturnsFocusedGermanTextAndPreservesRawReport()
    {
        const string rawText = "METAR ETHS 070720Z 17006KT 9999 FEW090 BKN240 16/08 Q1019 BLU+BLU+";
        AviationWeatherReport report = new(WeatherProduct.Metar, "ETHS", rawText, DateTimeOffset.UnixEpoch);
        AviationWeatherReportFormatter formatter = new();

        FormattedWeatherReport result = formatter.Format(report, AgentFocus.Weather, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal("Wetter: kein gemeldetes signifikantes Wetter", result.FormattedText);
        Assert.Equal(rawText, result.RawText);
        Assert.Equal(WeatherProduct.Metar, result.Product);
        Assert.Equal("ETHS", result.Station);
    }

    [Fact]
    public void Format_MetarFullFocus_DecodesRealEthsReport()
    {
        AviationWeatherReport report = new(WeatherProduct.Metar, "ETHS", "METAR ETHS 070720Z 17006KT 9999 FEW090 BKN240 16/08 Q1019 BLU+BLU+", DateTimeOffset.UnixEpoch);
        AviationWeatherReportFormatter formatter = new();

        FormattedWeatherReport result = formatter.Format(report, AgentFocus.Full, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("Wind: aus 170° mit 6 kt", result.FormattedText);
        Assert.Contains("Wolken: gering bewölkt in 9.000 ft AGL, durchbrochen in 24.000 ft AGL", result.FormattedText);
        Assert.Contains("Temperatur: 16 °C; Taupunkt: 8 °C", result.FormattedText);
        Assert.Contains("QNH: 1019 hPa", result.FormattedText);
    }

    [Fact]
    public void Format_TafValidityFocus_UsesDeterministicTafFormatterAndPreservesRawReport()
    {
        const string rawText = "TAF ETHS 070500Z 0706/0806 CAVOK";
        AviationWeatherReport report = new(WeatherProduct.Taf, "ETHS", rawText, DateTimeOffset.UnixEpoch);
        AviationWeatherReportFormatter formatter = new();

        FormattedWeatherReport result = formatter.Format(report, AgentFocus.Validity, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Contains("TAF ETHS", result.FormattedText);
        Assert.Contains("Ausgabe 07 05:00 UTC", result.FormattedText);
        Assert.Contains("Gültigkeit 07 06:00 UTC - 08 06:00 UTC", result.FormattedText);
        Assert.Equal(rawText, result.RawText);
    }
}
