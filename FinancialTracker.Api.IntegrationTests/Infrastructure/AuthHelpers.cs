using System.Net;
using Microsoft.AspNetCore.Identity;

namespace FinancialTracker.Api.IntegrationTests.Infrastructure;

/// <summary>A Google account as the fake Google scheme presents it.</summary>
public sealed record GoogleAccount(string Subject, string Email)
{
    public static GoogleAccount New() =>
        new(Random.Shared.NextInt64(100_000_000_000, 999_999_999_999).ToString(), $"user-{Guid.NewGuid():N}@example.test");
}

public sealed record SignedInUser(HttpClient Client, GoogleAccount Google);

public static class AuthHttpClientExtensions
{
    // Cookie authentication names a cookie ".AspNetCore." + its scheme, but Identity names its
    // external (Google) cookie after the scheme alone.
    public static readonly string AuthCookieName = ".AspNetCore." + IdentityConstants.ApplicationScheme;
    public static readonly string GoogleCookieName = IdentityConstants.ExternalScheme;

    /// <summary>Starts the Google sign-in flow as the given account and follows it to the app's callback.</summary>
    public static async Task<HttpResponseMessage> SignInWithGoogleAsync(
        this HttpClient client, GoogleAccount account, string returnUrl = "/") =>
        await client.GetAsync(await client.ConsentWithGoogleAsync(account, returnUrl));

    /// <summary>
    /// Goes through Google as the given account and returns the app callback the browser is sent to,
    /// without following it. An empty email means Google shares none.
    /// </summary>
    public static async Task<Uri> ConsentWithGoogleAsync(
        this HttpClient client, GoogleAccount account, string returnUrl = "/")
    {
        var challenge = new HttpRequestMessage(
            HttpMethod.Get, $"/api/auth/google/sign-in?returnUrl={Uri.EscapeDataString(returnUrl)}");
        challenge.Headers.Add(FakeGoogleHandler.SubjectHeader, account.Subject);
        if (account.Email.Length > 0)
        {
            challenge.Headers.Add(FakeGoogleHandler.EmailHeader, account.Email);
        }

        var toGoogle = await client.SendAsync(challenge);
        if (toGoogle.StatusCode is not HttpStatusCode.Redirect || toGoogle.Headers.Location is null)
        {
            var body = await toGoogle.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Expected a redirect to the callback, got {(int)toGoogle.StatusCode}: {body}");
        }

        return toGoogle.Headers.Location;
    }

    public static Task<HttpResponseMessage> PostSignOutAsync(this HttpClient client) =>
        client.PostAsync("/api/auth/sign-out", content: null);

    /// <summary>The auth cookie set by a response, as a "name=value" pair ready to replay.</summary>
    public static string AuthCookieFrom(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Select(cookie => cookie.Split(';')[0])
            .Single(pair => pair.StartsWith(AuthCookieName + "=", StringComparison.Ordinal));
}
