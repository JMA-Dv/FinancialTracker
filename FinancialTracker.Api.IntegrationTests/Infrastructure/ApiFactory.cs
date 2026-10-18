using FinancialTracker.Infrastructure;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

    /// <summary>A brand-new Google account, signed in for the first time, with the auth cookie held by the client.</summary>
    public async Task<SignedInUser> CreateSignedInUserAsync()
    {
        var client = CreateAnonymousClient();
        var google = GoogleAccount.New();

        var callback = await client.SignInWithGoogleAsync(google);
        if (callback.StatusCode is not System.Net.HttpStatusCode.Redirect)
        {
            throw new HttpRequestException($"Google sign-in failed: {(int)callback.StatusCode} {await callback.Content.ReadAsStringAsync()}");
        }

        return new SignedInUser(client, google);
    }

    /// <summary>The ids of every User the Google account signs in as. One, once it has signed in.</summary>
    public async Task<IReadOnlyList<Guid>> UserIdsForGoogleAsync(GoogleAccount google)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        return await dbContext.UserLogins
            .Where(login => login.LoginProvider == GoogleDefaults.AuthenticationScheme && login.ProviderKey == google.Subject)
            .Select(login => login.UserId)
            .ToListAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", database.GetConnectionString());
        builder.UseSetting("Authentication:Google:ClientId", "test-client-id");
        builder.UseSetting("Authentication:Google:ClientSecret", "test-client-secret");

        // Google itself is never called: its scheme is served by a fake that signs in whichever
        // Google account the test names.
        builder.ConfigureTestServices(services =>
            services.Configure<AuthenticationOptions>(options =>
                options.SchemeMap[GoogleDefaults.AuthenticationScheme].HandlerType = typeof(FakeGoogleHandler)));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
