using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class SignInTests(ApiFactory api)
{
    private const string LedgerEndpoint = "/api/user-settings";

    [Fact]
    public async Task A_visitor_who_is_not_signed_in_is_refused_by_ledger_endpoints()
    {
        var client = api.CreateAnonymousClient();

        var response = await client.GetAsync(LedgerEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_visitor_who_signs_up_and_signs_in_receives_an_auth_cookie_that_opens_ledger_endpoints()
    {
        var client = api.CreateAnonymousClient();
        var credentials = Credentials.New();

        var signUp = await client.PostSignUpAsync(credentials);
        Assert.Equal(HttpStatusCode.Created, signUp.StatusCode);

        var signIn = await client.PostSignInAsync(credentials);
        Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
        Assert.Contains(signIn.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var settings = await client.GetFromJsonAsync<UserSettings>(LedgerEndpoint);
        Assert.Equal(credentials.Email, settings?.Email);
    }

    [Fact]
    public async Task Signing_in_with_a_wrong_password_fails_the_same_way_as_an_unknown_email()
    {
        var client = api.CreateAnonymousClient();
        var registered = Credentials.New();
        await client.SignUpAsync(registered);

        var wrongPassword = await client.PostSignInAsync(registered with { Password = "Wr0ng-Password!" });
        var unknownEmail = await client.PostSignInAsync(Credentials.New());

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.False(wrongPassword.Headers.Contains("Set-Cookie"));
        Assert.Equal(await ProblemDetailOf(unknownEmail), await ProblemDetailOf(wrongPassword));
    }

    private static async Task<(string? Title, string? Detail)> ProblemDetailOf(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return (problem?.Title, problem?.Detail);
    }

    private sealed record ProblemDetails(string? Title, string? Detail);

    private sealed record UserSettings(string Email);
}
