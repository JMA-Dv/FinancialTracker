using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class GoogleSignInTests(ApiFactory api)
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
    public async Task A_visitors_first_Google_sign_in_gives_them_an_auth_cookie_that_opens_ledger_endpoints()
    {
        var client = api.CreateAnonymousClient();
        var google = GoogleAccount.New();

        var callback = await client.SignInWithGoogleAsync(google, returnUrl: "/accounts");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/accounts", callback.Headers.Location?.OriginalString);
        Assert.Contains(callback.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith(AuthHttpClientExtensions.AuthCookieName, StringComparison.Ordinal)
            && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var settings = await client.GetFromJsonAsync<UserSettings>(LedgerEndpoint);
        Assert.Equal(google.Email, settings?.Email);
    }

    [Fact]
    public async Task Signing_in_again_with_the_same_Google_account_opens_ledger_endpoints_again()
    {
        var google = GoogleAccount.New();
        await api.CreateAnonymousClient().SignInWithGoogleAsync(google);
        var laterDevice = api.CreateAnonymousClient();

        var callback = await laterDevice.SignInWithGoogleAsync(google);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Single(await api.UserIdsForGoogleAsync(google));
        var settings = await laterDevice.GetFromJsonAsync<UserSettings>(LedgerEndpoint);
        Assert.Equal(google.Email, settings?.Email);
    }

    [Fact]
    public async Task Two_Google_accounts_with_the_same_email_can_both_sign_in()
    {
        // Users are identified by Google account id, so a shared email is never a conflict.
        var first = GoogleAccount.New();
        var second = GoogleAccount.New() with { Email = first.Email };
        await api.CreateAnonymousClient().SignInWithGoogleAsync(first);
        var client = api.CreateAnonymousClient();

        var callback = await client.SignInWithGoogleAsync(second);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(LedgerEndpoint)).StatusCode);
        Assert.NotEqual(
            Assert.Single(await api.UserIdsForGoogleAsync(first)),
            Assert.Single(await api.UserIdsForGoogleAsync(second)));
    }

    [Theory]
    [InlineData("https://evil.example/phish")]
    [InlineData("//evil.example/phish")]
    public async Task Google_sign_in_never_sends_the_visitor_to_another_site(string returnUrl)
    {
        var client = api.CreateAnonymousClient();

        var callback = await client.SignInWithGoogleAsync(GoogleAccount.New(), returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/", callback.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Reaching_the_callback_without_signing_in_with_Google_fails_without_an_auth_cookie()
    {
        var client = api.CreateAnonymousClient();

        var callback = await client.GetAsync("/api/auth/google/callback?returnUrl=%2F");

        Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);
        Assert.False(callback.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.StartsWith(AuthHttpClientExtensions.AuthCookieName + "=", StringComparison.Ordinal)
                && !cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(LedgerEndpoint)).StatusCode);
    }

    [Fact]
    public async Task Finishing_a_first_Google_sign_in_twice_at_once_signs_in_both_times()
    {
        // A double-click, or two tabs coming back from Google together, on the very first sign-in.
        for (var round = 0; round < 5; round++)
        {
            var google = GoogleAccount.New();
            var firstTab = api.CreateAnonymousClient();
            var secondTab = api.CreateAnonymousClient();
            var firstCallback = await firstTab.ConsentWithGoogleAsync(google);
            var secondCallback = await secondTab.ConsentWithGoogleAsync(google);

            var callbacks = await Task.WhenAll(firstTab.GetAsync(firstCallback), secondTab.GetAsync(secondCallback));

            Assert.All(callbacks, callback => Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode));
            Assert.Equal(HttpStatusCode.OK, (await firstTab.GetAsync(LedgerEndpoint)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await secondTab.GetAsync(LedgerEndpoint)).StatusCode);
            Assert.Single(await api.UserIdsForGoogleAsync(google));
        }
    }

    [Fact]
    public async Task A_User_whose_Google_email_changed_sees_the_new_email_after_signing_in_again()
    {
        var google = GoogleAccount.New();
        await api.CreateAnonymousClient().SignInWithGoogleAsync(google);
        var userId = Assert.Single(await api.UserIdsForGoogleAsync(google));
        var changed = google with { Email = $"renamed-{Guid.NewGuid():N}@example.test" };
        var client = api.CreateAnonymousClient();

        await client.SignInWithGoogleAsync(changed);

        var settings = await client.GetFromJsonAsync<UserSettings>(LedgerEndpoint);
        Assert.Equal(changed.Email, settings?.Email);
        Assert.Equal(userId, Assert.Single(await api.UserIdsForGoogleAsync(changed)));
    }

    [Fact]
    public async Task A_failed_Google_sign_in_leaves_nothing_signed_in_behind()
    {
        var client = api.CreateAnonymousClient();
        var callbackUrl = await client.ConsentWithGoogleAsync(GoogleAccount.New() with { Email = "" });

        var failed = await client.GetAsync(callbackUrl);
        var retried = await client.GetAsync(callbackUrl);

        Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
        Assert.DoesNotContain(failed.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith(AuthHttpClientExtensions.AuthCookieName + "=", StringComparison.Ordinal));
        Assert.Contains(failed.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith(AuthHttpClientExtensions.GoogleCookieName + "=;", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.Unauthorized, retried.StatusCode);
    }

    private sealed record UserSettings(string Email);
}
