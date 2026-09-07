using System.Text.Json;
using MarcusRunge.AviationAgent.Core;
using MarcusRunge.AviationAgent.OnnxRuntime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntimeGenAI;

using OgaHandle ogaHandle = new();
IConfiguration configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false).Build();
OnnxRuntimeAgentModelOptions configuredOptions = configuration.GetRequiredSection("LocalAgentModel").Get<OnnxRuntimeAgentModelOptions>() ?? throw new InvalidOperationException("LocalAgentModel configuration is missing.");
OnnxRuntimeAgentModelOptions options = new()
{
    ModelDirectory = Path.IsPathRooted(configuredOptions.ModelDirectory) ? configuredOptions.ModelDirectory : Path.Combine(AppContext.BaseDirectory, configuredOptions.ModelDirectory),
    MaximumOutputTokens = configuredOptions.MaximumOutputTokens,
};

ServiceCollection services = new();
services.AddLocalAviationAgent(options);
await using ServiceProvider provider = services.BuildServiceProvider();
await using ILocalAgentModel model = provider.GetRequiredService<ILocalAgentModel>();

Console.WriteLine("Local Aviation Agent V2. Enter an empty line to exit.");
while (true)
{
    Console.Write("Request: ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    try
    {
        AgentDecision decision = await model.DecideAsync(input, CancellationToken.None);
        Console.WriteLine($"Decision: action={decision.Action.ToWireValue()}, station={decision.Station ?? "null"}, focus={decision.Focus.ToWireValue()}");
    }
    catch (Exception exception) when (exception is InvalidDataException or JsonException)
    {
        Console.WriteLine($"The model response was rejected: {exception.Message}");
    }
}
