using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Extensions;

internal static class InfrastructureExtensions
{
    internal static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["INFRASTRUCTURE_PROVIDER"] ?? "supabase";
        services.AddDbContexts(configuration, provider);
        services.AddAdapterInterfaces(configuration, provider);
        return services;
    }
}
