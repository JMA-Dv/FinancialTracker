using FinancialTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTracker.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "FinanceDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolved per context so the connection string can come from user secrets,
        // environment variables, or a test host, and is never needed in appsettings.json.
        services.AddDbContext<FinanceDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. " +
                    $"Set it with user secrets or the ConnectionStrings__{ConnectionStringName} environment variable.");

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
