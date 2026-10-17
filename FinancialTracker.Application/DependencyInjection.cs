using FinancialTracker.Application.UserSettings;
using FinancialTracker.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases are scoped like the stores they depend on: one per request.
        services.AddScoped<SignInWithGoogle>();
        services.AddScoped<GetUserSettings>();
        services.AddScoped<SetCurrency>();
        services.AddScoped<RequireCurrency>();

        return services;
    }
}
