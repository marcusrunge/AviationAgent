using System.Globalization;
using System.Text.Json;
using MarcusRunge.AviationAgent.AviationWeather;
using MarcusRunge.AviationAgent.Core;
using MarcusRunge.AviationAgent.OnnxRuntime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntimeGenAI;

using OgaHandle ogaHandle = new();
using CancellationTokenSource applicationCancellationTokenSource = new();

Console.CancelKeyPress += OnCancelKeyPress;

try
{
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false).Build();

    OnnxRuntimeAgentModelOptions modelOptions = CreateModelOptions(configuration);

    AviationWeatherClientOptions weatherOptions = CreateWeatherOptions(configuration);

    ServiceCollection services = new();

    services.AddSingleton(modelOptions);
    services.AddSingleton(weatherOptions);
    services.AddSingleton<HttpClient>();
    services.AddSingleton<IAviationWeatherClient>(
        static serviceProvider => new AviationWeatherGovClient(serviceProvider.GetRequiredService<HttpClient>(), serviceProvider.GetRequiredService<AviationWeatherClientOptions>()));
    services.AddSingleton<AgentDecisionRouter>();
    services.AddSingleton<ILocalAgentModel, OnnxRuntimeAgentModel>();

    await using ServiceProvider provider = services.BuildServiceProvider();

    ILocalAgentModel model = provider.GetRequiredService<ILocalAgentModel>();

    AgentDecisionRouter router = provider.GetRequiredService<AgentDecisionRouter>();

    await RunAsync(model, router, applicationCancellationTokenSource.Token);
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

return;

void OnCancelKeyPress(
    object? sender,
    ConsoleCancelEventArgs arguments)
{
    // Ctrl+C requests a controlled shutdown instead of terminating native
    // ONNX Runtime resources while a generation or HTTP request is active.
    arguments.Cancel = true;
    applicationCancellationTokenSource.Cancel();
}

static OnnxRuntimeAgentModelOptions CreateModelOptions(IConfiguration configuration)
{
    OnnxRuntimeAgentModelOptions configuredOptions = configuration.GetRequiredSection("LocalAgentModel").Get<OnnxRuntimeAgentModelOptions>() ?? throw new InvalidOperationException("LocalAgentModel configuration is missing.");

    if (string.IsNullOrWhiteSpace(
        configuredOptions.ModelDirectory))
    {
        throw new InvalidOperationException("LocalAgentModel:ModelDirectory is missing.");
    }

    string modelDirectory = Path.IsPathRooted(configuredOptions.ModelDirectory) ? configuredOptions.ModelDirectory : Path.Combine(AppContext.BaseDirectory, configuredOptions.ModelDirectory);

    return new OnnxRuntimeAgentModelOptions
    {
        ModelDirectory = Path.GetFullPath(modelDirectory),
        MaximumOutputTokens = configuredOptions.MaximumOutputTokens,
    };
}

static AviationWeatherClientOptions CreateWeatherOptions(IConfiguration configuration)
{
    IConfigurationSection section = configuration.GetRequiredSection("AviationWeather");

    string baseAddressValue = section["BaseAddress"] ?? throw new InvalidOperationException("AviationWeather:BaseAddress is missing.");

    string timeoutValue = section["RequestTimeoutSeconds"] ?? throw new InvalidOperationException("AviationWeather:RequestTimeoutSeconds is missing.");

    if (!Uri.TryCreate(baseAddressValue, UriKind.Absolute, out Uri? baseAddress))
    {
        throw new InvalidOperationException("AviationWeather:BaseAddress must be an absolute URI.");
    }

    if (!int.TryParse(timeoutValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int requestTimeoutSeconds) || requestTimeoutSeconds <= 0)
    {
        throw new InvalidOperationException("AviationWeather:RequestTimeoutSeconds must be greater than zero.");
    }

    return new AviationWeatherClientOptions
    {
        BaseAddress = baseAddress,
        RequestTimeout = TimeSpan.FromSeconds(requestTimeoutSeconds),
    };
}

static async Task RunAsync(ILocalAgentModel model, AgentDecisionRouter router, CancellationToken cancellationToken)
{
    Console.WriteLine("Local Aviation Agent V2");
    Console.WriteLine("Enter an empty line to exit. Press Ctrl+C to cancel.");
    Console.WriteLine();

    while (!cancellationToken.IsCancellationRequested)
    {
        Console.Write("Request: ");

        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        try
        {
            AgentDecision decision = await model.DecideAsync(input, cancellationToken);

            PrintDecision(decision);

            AgentRouteResult routeResult = await router.RouteAsync(decision, cancellationToken);

            PrintReports(routeResult);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("The operation was cancelled.");
            return;
        }
        catch (AviationWeatherClientException exception)
        {
            PrintWeatherError(exception);
        }
        catch (InvalidDataException exception)
        {
            Console.WriteLine($"The model response was rejected: {exception.Message}");
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"The model returned invalid JSON: {exception.Message}");
        }

        Console.WriteLine();
    }
}

static void PrintDecision(AgentDecision decision)
{
    Console.WriteLine(
        $"Decision: action={decision.Action.ToWireValue()}, " + $"station={decision.Station ?? "null"}, " + $"focus={decision.Focus.ToWireValue()}");
}

static void PrintReports(
    AgentRouteResult result)
{
    if (!result.HasReports)
    {
        PrintNonWeatherResult(result.Decision);
        return;
    }

    foreach (AviationWeatherReport report
        in result.Reports)
    {
        Console.WriteLine();
        Console.WriteLine($"{report.Product}:");
        Console.WriteLine(report.RawText);
        Console.WriteLine($"Retrieved at: " + $"{report.RetrievedAt:yyyy-MM-dd HH:mm:ss} UTC");
    }
}

static void PrintNonWeatherResult(
    AgentDecision decision)
{
    switch (decision.Action)
    {
        case AgentAction.Unknown:
            Console.WriteLine("The request could not be assigned to a station and weather product.");
            break;

        case AgentAction.UnsupportedOperationalDecision:
            Console.WriteLine("The agent cannot make operational start, landing, " + "flight-release, minima, or go/no-go decisions.");
            break;

        case AgentAction.ExplainPreviousResult:
        case AgentAction.FilterPreviousResult:
            Console.WriteLine("Follow-up processing is not implemented yet.");
            break;

        default:
            Console.WriteLine("No aviation weather report was retrieved.");
            break;
    }
}

static void PrintWeatherError(AviationWeatherClientException exception)
{
    if (exception.StatusCode is not null)
    {
        Console.WriteLine($"Weather service error " + $"({(int)exception.StatusCode.Value}): " + $"{exception.Message}");

        return;
    }

    Console.WriteLine($"Weather service error: {exception.Message}");
}