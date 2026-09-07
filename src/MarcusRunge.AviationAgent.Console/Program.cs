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
    CultureInfo outputCulture = CreateOutputCulture(configuration);
    ServiceCollection services = new();
    services.AddSingleton(modelOptions);
    services.AddSingleton(weatherOptions);
    services.AddSingleton<HttpClient>();
    services.AddSingleton<IAviationWeatherClient>(static serviceProvider => new AviationWeatherGovClient(serviceProvider.GetRequiredService<HttpClient>(), serviceProvider.GetRequiredService<AviationWeatherClientOptions>()));
    services.AddSingleton<IAviationWeatherReportFormatter, AviationWeatherReportFormatter>();
    services.AddSingleton<AgentDecisionRouter>();
    services.AddSingleton<ILocalAgentModel, OnnxRuntimeAgentModel>();
    await using ServiceProvider provider = services.BuildServiceProvider();
    await RunAsync(provider.GetRequiredService<ILocalAgentModel>(), provider.GetRequiredService<AgentDecisionRouter>(), provider.GetRequiredService<IAviationWeatherReportFormatter>(), outputCulture, applicationCancellationTokenSource.Token);
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

return;

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs arguments)
{
    // Ctrl+C requests a controlled shutdown instead of terminating native
    // ONNX Runtime resources during generation or an HTTP operation.
    arguments.Cancel = true;
    applicationCancellationTokenSource.Cancel();
}

static OnnxRuntimeAgentModelOptions CreateModelOptions(IConfiguration configuration)
{
    OnnxRuntimeAgentModelOptions configuredOptions = configuration.GetRequiredSection("LocalAgentModel").Get<OnnxRuntimeAgentModelOptions>() ?? throw new InvalidOperationException("LocalAgentModel configuration is missing.");
    if (string.IsNullOrWhiteSpace(configuredOptions.ModelDirectory)) throw new InvalidOperationException("LocalAgentModel:ModelDirectory is missing.");
    string modelDirectory = Path.IsPathRooted(configuredOptions.ModelDirectory) ? configuredOptions.ModelDirectory : Path.Combine(AppContext.BaseDirectory, configuredOptions.ModelDirectory);
    return new OnnxRuntimeAgentModelOptions { ModelDirectory = Path.GetFullPath(modelDirectory), MaximumOutputTokens = configuredOptions.MaximumOutputTokens };
}

static AviationWeatherClientOptions CreateWeatherOptions(IConfiguration configuration)
{
    IConfigurationSection section = configuration.GetRequiredSection("AviationWeather");
    string baseAddressValue = section["BaseAddress"] ?? throw new InvalidOperationException("AviationWeather:BaseAddress is missing.");
    string timeoutValue = section["RequestTimeoutSeconds"] ?? throw new InvalidOperationException("AviationWeather:RequestTimeoutSeconds is missing.");
    if (!Uri.TryCreate(baseAddressValue, UriKind.Absolute, out Uri? baseAddress)) throw new InvalidOperationException("AviationWeather:BaseAddress must be an absolute URI.");
    if (!int.TryParse(timeoutValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int requestTimeoutSeconds) || requestTimeoutSeconds <= 0) throw new InvalidOperationException("AviationWeather:RequestTimeoutSeconds must be greater than zero.");
    return new AviationWeatherClientOptions { BaseAddress = baseAddress, RequestTimeout = TimeSpan.FromSeconds(requestTimeoutSeconds) };
}

static CultureInfo CreateOutputCulture(IConfiguration configuration)
{
    string cultureName = configuration["Output:Culture"] ?? "de-DE";
    try { return CultureInfo.GetCultureInfo(cultureName); }
    catch (CultureNotFoundException exception) { throw new InvalidOperationException($"Output:Culture '{cultureName}' is invalid.", exception); }
}

static async Task RunAsync(ILocalAgentModel model, AgentDecisionRouter router, IAviationWeatherReportFormatter formatter, CultureInfo outputCulture, CancellationToken cancellationToken)
{
    Console.WriteLine("Local Aviation Agent V2");
    Console.WriteLine("Enter an empty line to exit. Press Ctrl+C to cancel.");
    Console.WriteLine();

    while (!cancellationToken.IsCancellationRequested)
    {
        Console.Write("Request: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return;

        try
        {
            AgentDecision decision = await model.DecideAsync(input, cancellationToken);
            PrintDecision(decision);
            AgentRouteResult routeResult = await router.RouteAsync(decision, cancellationToken);
            PrintReports(routeResult, formatter, outputCulture);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("The operation was cancelled.");
            return;
        }
        catch (AviationWeatherClientException exception) { PrintWeatherError(exception); }
        catch (InvalidDataException exception) { Console.WriteLine($"The weather or model data was rejected: {exception.Message}"); }
        catch (JsonException exception) { Console.WriteLine($"The model returned invalid JSON: {exception.Message}"); }

        Console.WriteLine();
    }
}

static void PrintDecision(AgentDecision decision) => Console.WriteLine($"Decision: action={decision.Action.ToWireValue()}, station={decision.Station ?? "null"}, focus={decision.Focus.ToWireValue()}");

static void PrintReports(AgentRouteResult result, IAviationWeatherReportFormatter formatter, CultureInfo culture)
{
    if (!result.HasReports) { PrintNonWeatherResult(result.Decision); return; }

    foreach (AviationWeatherReport report in result.Reports)
    {
        FormattedWeatherReport formattedReport = formatter.Format(report, result.Decision.Focus, culture);
        Console.WriteLine();
        Console.WriteLine($"{formattedReport.Product} {formattedReport.Station}:");
        Console.WriteLine(formattedReport.FormattedText);
        Console.WriteLine();
        Console.WriteLine("Raw report:");
        Console.WriteLine(formattedReport.RawText);
        Console.WriteLine($"Retrieved at: {formattedReport.RetrievedAt:yyyy-MM-dd HH:mm:ss} UTC");
    }
}

static void PrintNonWeatherResult(AgentDecision decision)
{
    string message = decision.Action switch
    {
        AgentAction.Unknown => "The request could not be assigned to a station and weather product.",
        AgentAction.UnsupportedOperationalDecision => "The agent cannot make operational start, landing, flight-release, minima, or go/no-go decisions.",
        AgentAction.ExplainPreviousResult or AgentAction.FilterPreviousResult => "Follow-up processing is not implemented yet.",
        _ => "No aviation weather report was retrieved.",
    };
    Console.WriteLine(message);
}

static void PrintWeatherError(AviationWeatherClientException exception) => Console.WriteLine(exception.StatusCode is null ? $"Weather service error: {exception.Message}" : $"Weather service error ({(int)exception.StatusCode.Value}): {exception.Message}");
