using ForaChallenge.Application.Services;
using ForaChallenge.Infrastructure.SecEdgar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForaChallenge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("SecEdgar");
        services.AddScoped<ISecEdgarApiClient, SecEdgarApiClient>();

        return services;
    }
}
