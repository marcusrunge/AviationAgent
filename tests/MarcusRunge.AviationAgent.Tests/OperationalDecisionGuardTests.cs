using MarcusRunge.AviationAgent.Core;
using Xunit;

namespace MarcusRunge.AviationAgent.Tests;

public sealed class OperationalDecisionGuardTests
{
    [Fact]
    public void TryReject_SafeLandingRequest_ReturnsRejection()
    {
        AgentDecision? decision = OperationalDecisionGuard.TryReject("Kann ich sicher in ETHS landen?");
        Assert.Equal(new AgentDecision("unsupported_operational_decision", "ETHS", "none"), decision);
    }

    [Fact]
    public void TryReject_NormalWeatherRequest_ReturnsNull() => Assert.Null(OperationalDecisionGuard.TryReject("TAF ETHS Schwerpunkt Wind"));
}
