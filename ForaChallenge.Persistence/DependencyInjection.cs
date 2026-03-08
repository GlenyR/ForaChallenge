using ForaChallenge.Application.Repositories;
using ForaChallenge.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace ForaChallenge.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data source=fora.db";
        services.AddDbContext<ForaDbContext>(o => o.UseSqlite(connectionString));

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICikImportRepository, CikImportRepository>();

        return services;
    }
}

