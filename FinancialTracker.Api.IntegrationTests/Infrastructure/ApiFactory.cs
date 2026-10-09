using FinancialTracker.Infrastructure;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FinancialTracker.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a disposable Postgres container, shared by every test in the run.
/// Tests stay isolated by creating their own User rather than resetting the database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await database.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
    }

    /// <summary>A client with no auth cookie.</summary>
    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>A brand-new User, signed up and signed in, with the auth cookie held by the client.</summary>
    public async Task<SignedInUser> CreateSignedInUserAsync()
    {
        var client = CreateAnonymousClient();
        var credentials = Credentials.New();

        await client.SignUpAsync(credentials);
        await client.SignInAsync(credentials);

        return new SignedInUser(client, credentials);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", database.GetConnectionString());
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
