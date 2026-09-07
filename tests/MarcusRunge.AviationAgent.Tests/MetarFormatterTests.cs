using System.Globalization;
using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class MetarFormatterTests
{
    private static readonly DecodedMetar Metar = MetarParser.Parse("METAR EDDV 070750Z 24012G22KT 9999 -RA BKN018 OVC035 14/11 Q1008 NOSIG=");

    [Fact]
    public void Format_WindInGerman_ReturnsLocalizedWindAndGusts()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Wind, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("Wind: aus 240° mit 12 kt, Böen bis 22 kt", result);
    }

    [Fact]
    public void Format_PressureInEnglish_ReturnsLocalizedQnh()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Pressure, CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("QNH: 1008 hPa", result);
    }

    [Fact]
    public void Format_CloudsInGerman_ReturnsLocalizedLayers()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Clouds, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("Wolken: durchbrochen in 1.800 ft AGL, bedeckt in 3.500 ft AGL", result);
    }

    [Fact]
    public void Format_WeatherInEnglish_TranslatesKnownCodeAndRetainsOriginalCode()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Weather, CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("Weather: light rain (-RA)", result);
    }

    [Fact]
    public void Format_FullInGerman_ContainsAllSupportedSections()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Full, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Contains("Station: EDDV", result);
        Assert.Contains("Beobachtung: Tag 07 um 07:50 UTC", result);
        Assert.Contains("Sicht: mindestens 10 km", result);
        Assert.Contains("Temperatur: 14 °C; Taupunkt: 11 °C", result);
    }

    [Fact]
    public void Format_Changes_DoesNotInventTrendInformation()
    {
        string result = MetarFormatter.Format(Metar, AgentFocus.Changes, CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("Aus der dekodierten METAR-Meldung werden keine Änderungsinformationen abgeleitet.", result);
    }
}
