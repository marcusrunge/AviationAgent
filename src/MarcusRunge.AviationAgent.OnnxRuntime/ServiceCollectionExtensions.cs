using MarcusRunge.AviationAgent.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MarcusRunge.AviationAgent.OnnxRuntime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalAviationAgent(this IServiceCollection services, OnnxRuntimeAgentModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton(options);
        services.AddSingleton<ILocalAgentModel, OnnxRuntimeAgentModel>();
        return services;
    }
}
