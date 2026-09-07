using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class MetarParserTests
{
    [Fact]
    public void Parse_RepresentativeEuropeanMetar_DecodesSupportedGroups()
    {
        DecodedMetar result = MetarParser.Parse("METAR EDDV 070750Z 24012G22KT 9999 -RA BKN018 OVC035 14/11 Q1008 NOSIG=");

        Assert.Equal("EDDV", result.Station);
        Assert.Equal(7, result.ObservationDay);
        Assert.Equal(new TimeOnly(7, 50), result.ObservationTimeUtc);
        Assert.Equal(new MetarWind(240, 12, 22, false), result.Wind);
        Assert.Equal(10_000, result.VisibilityMeters);
        Assert.Equal(["-RA"], result.WeatherPhenomena);
        Assert.Equal(
            [
                new MetarCloudLayer("BKN", 1_800, null),
                new MetarCloudLayer("OVC", 3_500, null),
            ],
            result.CloudLayers);
        Assert.Equal(14, result.TemperatureCelsius);
        Assert.Equal(11, result.DewPointCelsius);
        Assert.Equal(1008, result.QnhHectopascals);
    }

    [Fact]
    public void Parse_CavokAndVariableWind_DecodesSpecialGroups()
    {
        DecodedMetar result = MetarParser.Parse("ETHS 071020Z VRB03KT CAVOK M02/M05 Q1024");

        Assert.True(result.IsCavok);
        Assert.Equal(10_000, result.VisibilityMeters);
        Assert.Equal(new MetarWind(null, 3, null, true), result.Wind);
        Assert.Equal(-2, result.TemperatureCelsius);
        Assert.Equal(-5, result.DewPointCelsius);
        Assert.Equal(1024, result.QnhHectopascals);
    }

    [Fact]
    public void Parse_MetersPerSecond_ConvertsWindToKnots()
    {
        DecodedMetar result = MetarParser.Parse("METAR LBBG 041600Z 12012MPS 1400 +SN BKN022 M04/M07 Q1020");

        Assert.Equal(new MetarWind(120, 23, null, false), result.Wind);
    }

    [Fact]
    public void Parse_CloudType_DecodesCumulonimbus()
    {
        DecodedMetar result = MetarParser.Parse("METAR EDDF 071020Z 18010KT 8000 TSRA BKN025CB 18/15 Q1009");

        Assert.Equal(new MetarCloudLayer("BKN", 2_500, "CB"), Assert.Single(result.CloudLayers));
        Assert.Equal(["TSRA"], result.WeatherPhenomena);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_EmptyInput_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => MetarParser.Parse(input));
    }

    [Theory]
    [InlineData("METAR")]
    [InlineData("INVALID 071020Z")]
    [InlineData("EDDV INVALID")]
    public void Parse_MalformedMetar_ThrowsInvalidDataException(string input)
    {
        Assert.Throws<InvalidDataException>(() => MetarParser.Parse(input));
    }
}