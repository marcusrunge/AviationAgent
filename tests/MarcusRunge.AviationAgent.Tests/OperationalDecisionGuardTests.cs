using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class OperationalDecisionGuardTests
{
    [Theory]
    [InlineData("Kann ich sicher in ETHS landen?", "ETHS")]
    [InlineData("Kann ich in EDDH landen?", "EDDH")]
    [InlineData("Darf ich nach EDDF fliegen?", "EDDF")]
    [InlineData("Soll ich in EDDL starten?", "EDDL")]
    [InlineData("Kann ich von EDDM abfliegen?", "EDDM")]
    public void TryReject_PermissionRequest_ReturnsTypedRejection(string input, string expectedStation)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.Equal(new AgentDecision(AgentAction.UnsupportedOperationalDecision, expectedStation, AgentFocus.None), decision);
    }

    [Theory]
    [InlineData("Kann ich sicher in eths landen?", "ETHS")]
    [InlineData("Darf ich nach eddf fliegen?", "EDDF")]
    [InlineData("Soll ich in eddl starten?", "EDDL")]
    public void TryReject_LowercaseStation_NormalizesStation(string input, string expectedStation)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.Equal(new AgentDecision(AgentAction.UnsupportedOperationalDecision, expectedStation, AgentFocus.None), decision);
    }

    [Theory]
    [InlineData("Ist der Anflug auf EDDS zulässig?", "EDDS")]
    [InlineData("Ist eine Landung in EDDV möglich?", "EDDV")]
    [InlineData("Gib den Flug nach EDDP frei.", "EDDP")]
    [InlineData("Triff eine Go-No-Go-Entscheidung für EDDN.", "EDDN")]
    [InlineData("Sind meine Minima für EDDR erfüllt?", "EDDR")]
    [InlineData("Brauche ich einen Alternate für EDDC?", "EDDC")]
    public void TryReject_OperationalRequest_ReturnsTypedRejection(string input, string expectedStation)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.Equal(new AgentDecision(AgentAction.UnsupportedOperationalDecision, expectedStation, AgentFocus.None), decision);
    }

    [Theory]
    [InlineData("Kann ich sicher landen?")]
    [InlineData("Darf ich fliegen?")]
    [InlineData("Soll ich starten?")]
    [InlineData("Triff eine Go-No-Go-Entscheidung.")]
    [InlineData("Ist die Landung zulässig?")]
    public void TryReject_OperationalRequestWithoutStation_ReturnsRejection(string input)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.Equal(new AgentDecision(AgentAction.UnsupportedOperationalDecision, null, AgentFocus.None), decision);
    }

    [Theory]
    [InlineData("TAF ETHS. Schwerpunkt: Wind und Böen.")]
    [InlineData("METAR EDDH. Schwerpunkt: Sichtweite.")]
    [InlineData("Zeige Beobachtung und Prognose für EDDF.")]
    [InlineData("Wie entwickelt sich das Wetter in EDDL?")]
    [InlineData("Welche Sicht wird für EDDM erwartet?")]
    [InlineData("Hole beide Wettermeldungen für EDDS.")]
    public void TryReject_NormalWeatherRequest_ReturnsNull(string input)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.Null(decision);
    }

    [Theory]
    [InlineData("Kann ich sicher landen?")]
    [InlineData("Darf ich fliegen?")]
    [InlineData("Gib den Flug frei.")]
    public void TryReject_CommonFourLetterWords_AreNotStations(string input)
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject(input);

        Assert.NotNull(decision);
        Assert.Null(decision.Station);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryReject_EmptyInput_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => OperationalDecisionGuard.TryReject(input));
    }
}