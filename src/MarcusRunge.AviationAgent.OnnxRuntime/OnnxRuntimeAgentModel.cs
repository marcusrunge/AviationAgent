using MarcusRunge.AviationAgent.Core;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace MarcusRunge.AviationAgent.OnnxRuntime;

public sealed class OnnxRuntimeAgentModel : ILocalAgentModel
{
    private readonly Model _model;
    private readonly Tokenizer _tokenizer;
    private readonly string _contract;
    private readonly int _maximumOutputTokens;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public OnnxRuntimeAgentModel(OnnxRuntimeAgentModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string modelDirectory = Path.GetFullPath(options.ModelDirectory);
        if (!Directory.Exists(modelDirectory)) throw new DirectoryNotFoundException($"Model directory not found: {modelDirectory}");

        string contractPath = Path.Combine(modelDirectory, "agent_contract_v2.txt");
        if (!File.Exists(contractPath)) throw new FileNotFoundException("The V2 agent contract is missing.", contractPath);
        if (options.MaximumOutputTokens <= 0) throw new ArgumentOutOfRangeException(nameof(options), options.MaximumOutputTokens, "MaximumOutputTokens must be greater than zero.");

        _maximumOutputTokens = options.MaximumOutputTokens;
        _contract = File.ReadAllText(contractPath).Trim();
        _model = new Model(modelDirectory);
        _tokenizer = new Tokenizer(_model);
    }

    public async Task<AgentDecision> DecideAsync(string input, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        AgentDecision? rejection = OperationalDecisionGuard.TryReject(input);
        if (rejection is not null) return rejection;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => Generate(input, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private AgentDecision Generate(string input, CancellationToken cancellationToken)
    {
        // Preserve the exact ChatML framing that was used for V2 training and Python verification.
        string prompt = $"<|im_start|>system\n{_contract}<|im_end|>\n<|im_start|>user\n{input}<|im_end|>\n<|im_start|>assistant\n";
        using Sequences sequences = _tokenizer.Encode(prompt);
        int promptLength = sequences[0].Length;
        using GeneratorParams parameters = new(_model);
        parameters.SetSearchOption("max_length", promptLength + _maximumOutputTokens);
        parameters.SetSearchOption("do_sample", false);
        using Generator generator = new(_model, parameters);
        generator.AppendTokenSequences(sequences);

        // The native API generates synchronously. Checking cancellation after each token keeps cancellation bounded.
        while (!generator.IsDone())
        {
            cancellationToken.ThrowIfCancellationRequested();
            generator.GenerateNextToken();
        }

        ReadOnlySpan<int> generatedSequence = generator.GetSequence(0);
        string output = _tokenizer.Decode(generatedSequence[promptLength..]).Trim();
        return AgentDecisionParser.Parse(output);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _disposed = true;
        _tokenizer.Dispose();
        _model.Dispose();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
