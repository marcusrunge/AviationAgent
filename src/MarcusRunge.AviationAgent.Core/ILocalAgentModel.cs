namespace MarcusRunge.AviationAgent.Core;

public interface ILocalAgentModel : IAsyncDisposable
{
    Task<AgentDecision> DecideAsync(string input, CancellationToken cancellationToken);
}
