using System.Net;
using FinancialTracker.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialTracker.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class SignOutTests(ApiFactory api)
{
    private const string LedgerEndpoint = "/api/user-settings";

    [Fact]
    public async Task A_signed_in_User_who_signs_out_is_refused_by_ledger_endpoints()
    {
        var user = await api.CreateSignedInUserAsync();

        var signOut = await user.Client.PostSignOutAsync();
        var afterSignOut = await user.Client.GetAsync(LedgerEndpoint);

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);
    }

    [Fact]
    public async Task The_cookie_a_User_held_before_signing_out_no_longer_grants_access()
    {
        // Handle the cookie by hand so it can be replayed after the browser would have dropped it.
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var credentials = Credentials.New();
        await client.SignUpAsync(credentials);
        var signIn = await client.PostSignInAsync(credentials);
        var authCookie = signIn.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        Assert.Equal(HttpStatusCode.OK, (await SendWithCookie(client, HttpMethod.Get, LedgerEndpoint, authCookie)).StatusCode);

        var signOut = await SendWithCookie(client, HttpMethod.Post, "/api/auth/sign-out", authCookie);
        var replayed = await SendWithCookie(client, HttpMethod.Get, LedgerEndpoint, authCookie);

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replayed.StatusCode);
    }

    private static Task<HttpResponseMessage> SendWithCookie(HttpClient client, HttpMethod method, string uri, string cookie)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }
}
