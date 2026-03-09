using ForaChallenge.Application.Services;
using ForaChallenge.Infrastructure.Edgar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForaChallenge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("SecEdgar");
        services.AddScoped<ISecEdgarApiClient, SecEdgarApiClient>();
        services.AddScoped<IEdgarCompanyFactsMapper, EdgarCompanyFactsMapper>();
        services.AddScoped<IEdgarImportService, EdgarImportService>();
        services.AddHostedService<EdgarImportHostedService>();

        return services;
    }
}
