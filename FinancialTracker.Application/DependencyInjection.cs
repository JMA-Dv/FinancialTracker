using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Categories;
using FinancialTracker.Application.Payees;
using FinancialTracker.Application.Transactions;
using FinancialTracker.Application.Transfers;
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
        services.AddScoped<CreateBalanceAdjustment>();
        services.AddScoped<GetBalanceAdjustment>();
        services.AddScoped<ListBalanceAdjustments>();
        services.AddScoped<EditBalanceAdjustment>();
        services.AddScoped<DeleteBalanceAdjustment>();
        services.AddScoped<GetAccountBalance>();
        services.AddScoped<GetAccountHistory>();
        services.AddScoped<CreateCategory>();
        services.AddScoped<ListCategories>();
        services.AddScoped<RenameCategory>();
        services.AddScoped<MoveCategory>();
        services.AddScoped<ArchiveCategory>();
        services.AddScoped<AccountSummarizer>();
        services.AddScoped<CreateTransaction>();
        services.AddScoped<ListTransactions>();
        services.AddScoped<EditTransaction>();
        services.AddScoped<DeleteTransaction>();
        services.AddScoped<ListPayees>();
        services.AddScoped<RenamePayee>();
        services.AddScoped<CreateTransfer>();
        services.AddScoped<GetTransfer>();
        services.AddScoped<ListTransfers>();
        services.AddScoped<EditTransfer>();
        services.AddScoped<DeleteTransfer>();

        // "Today" comes from one clock, so tests can move it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
