using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class AgentDecisionParserTests
{
    [Fact]
    public void Parse_ValidDecision_ReturnsDecision()
    {
        const string json = """{"action":"get_taf","station":"ETHS","focus":"wind"}""";

        AgentDecision decision = AgentDecisionParser.Parse(json);

        Assert.Equal(new AgentDecision("get_taf", "ETHS", "wind"), decision);
    }

    [Fact]
    public void Parse_AdditionalProperty_Throws()
    {
        const string json = """{"action":"get_taf","station":"ETHS","focus":"wind","url":"x"}""";

        Assert.Throws<InvalidDataException>(() => AgentDecisionParser.Parse(json));
    }

    [Fact]
    public void Parse_WeatherActionWithoutStation_Throws()
    {
        const string json = """{"action":"get_taf","station":null,"focus":"wind"}""";

        Assert.Throws<InvalidDataException>(() => AgentDecisionParser.Parse(json));
    }
}
