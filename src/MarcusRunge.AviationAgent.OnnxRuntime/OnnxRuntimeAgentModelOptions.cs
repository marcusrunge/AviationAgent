namespace MarcusRunge.AviationAgent.OnnxRuntime;

public sealed class OnnxRuntimeAgentModelOptions
{
    public required string ModelDirectory { get; init; }
    public int MaximumOutputTokens { get; init; } = 96;
}
