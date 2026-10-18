using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.UserSettings;
using FinancialTracker.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinancialTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases are scoped like the stores they depend on: one per request.
        services.AddScoped<SignInWithGoogle>();
        services.AddScoped<GetUserSettings>();
        services.AddScoped<SetCurrency>();
        services.AddScoped<OpenAccount>();
        services.AddScoped<ListAccounts>();
        services.AddScoped<GetAccount>();
        services.AddScoped<RenameAccount>();
        services.AddScoped<ChangeAccountCurrency>();

        // "Today" comes from one clock, so tests can move it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
