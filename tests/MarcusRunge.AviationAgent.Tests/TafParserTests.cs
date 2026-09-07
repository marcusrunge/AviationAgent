using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class TafParserTests
{
    [Fact]
    public void Parse_InternationalTaf_DecodesTimeline()
    {
        const string raw = "TAF EDDV 121100Z 1212/1312 25012KT 9999 FEW040 TEMPO 1216/1219 25018G28KT 6000 SHRA BKN025CB BECMG 1306/1308 24010KT 9999 SCT040";
        DecodedTaf taf = TafParser.Parse(raw);
        Assert.Equal("EDDV", taf.Station);
        Assert.Equal(new TafTimePoint(12, 11), taf.IssueTime);
        Assert.Equal(new TafPeriod(new TafTimePoint(12, 12), new TafTimePoint(13, 12)), taf.Validity);
        Assert.Equal(new MetarWind(250, 12, null, false), taf.BaseForecast.Conditions.Wind);
        Assert.Collection(taf.ChangeGroups, group => { Assert.Equal(TafChangeKind.Temporary, group.Kind); Assert.Equal(new MetarWind(250, 18, 28, false), group.Conditions.Wind); Assert.Equal(["SHRA"], group.Conditions.WeatherPhenomena); }, group => Assert.Equal(TafChangeKind.Becoming, group.Kind));
    }

    [Fact]
    public void Parse_FmAndProbabilityTemporary_DecodesChangeSemantics()
    {
        const string raw = "TAF EDDF 121100Z 1212/1318 22008KT CAVOK FM121800 26015G25KT 7000 -RA BKN020 PROB30 TEMPO 1302/1306 2000 TSRA BKN008CB";
        DecodedTaf taf = TafParser.Parse(raw);
        Assert.True(taf.BaseForecast.Conditions.IsCavok);
        Assert.Equal(TafChangeKind.From, taf.ChangeGroups[0].Kind);
        Assert.Equal(new TafTimePoint(12, 18), taf.ChangeGroups[0].From);
        Assert.Equal(TafChangeKind.ProbabilityTemporary, taf.ChangeGroups[1].Kind);
        Assert.Equal(30, taf.ChangeGroups[1].ProbabilityPercent);
    }

    [Fact]
    public void Parse_AmendedCancelledTaf_PreservesStatus()
    {
        DecodedTaf taf = TafParser.Parse("TAF AMD ETHS 121100Z 1212/1312 CNL");
        Assert.True(taf.IsAmended);
        Assert.True(taf.IsCancelled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TAF")]
    [InlineData("TAF INVALID 121100Z 1212/1312 CAVOK")]
    [InlineData("TAF EDDV INVALID 1212/1312 CAVOK")]
    public void Parse_InvalidInput_Throws(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) Assert.Throws<ArgumentException>(() => TafParser.Parse(raw));
        else Assert.Throws<InvalidDataException>(() => TafParser.Parse(raw));
    }
}
