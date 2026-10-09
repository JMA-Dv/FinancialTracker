using System.Net.Http.Json;

namespace FinancialTracker.Api.IntegrationTests.Infrastructure;

public sealed record Credentials(string Email, string Password)
{
    public static Credentials New() => new($"user-{Guid.NewGuid():N}@example.test", "Corr3ct-Horse!");
}

public sealed record SignedInUser(HttpClient Client, Credentials Credentials);

public static class AuthHttpClientExtensions
{
    public static Task<HttpResponseMessage> PostSignUpAsync(this HttpClient client, Credentials credentials) =>
        client.PostAsJsonAsync("/api/auth/sign-up", new { credentials.Email, credentials.Password });

    public static Task<HttpResponseMessage> PostSignInAsync(this HttpClient client, Credentials credentials) =>
        client.PostAsJsonAsync("/api/auth/sign-in", new { credentials.Email, credentials.Password });

    public static Task<HttpResponseMessage> PostSignOutAsync(this HttpClient client) =>
        client.PostAsync("/api/auth/sign-out", content: null);

    public static async Task SignUpAsync(this HttpClient client, Credentials credentials)
    {
        var response = await client.PostSignUpAsync(credentials);
        await response.EnsureSuccessWithBodyAsync();
    }

    public static async Task SignInAsync(this HttpClient client, Credentials credentials)
    {
        var response = await client.PostSignInAsync(credentials);
        await response.EnsureSuccessWithBodyAsync();
    }

    private static async Task EnsureSuccessWithBodyAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode}: {body}");
        }
    }
}
