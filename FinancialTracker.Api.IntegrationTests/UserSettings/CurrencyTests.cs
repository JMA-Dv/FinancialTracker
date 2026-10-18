using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.UserSettings;

[Collection(ApiCollection.Name)]
public sealed class CurrencyTests(ApiFactory api)
{
    private const string SettingsEndpoint = "/api/user-settings";

    [Fact]
    public async Task A_new_User_has_no_Currency_yet()
    {
        var user = await api.CreateSignedInUserAsync();

        var settings = await user.Client.GetFromJsonAsync<Settings>(SettingsEndpoint);

        Assert.Equal(user.Google.Email, settings?.Email);
        Assert.Null(settings?.Currency);
    }

    [Fact]
    public async Task A_User_who_sets_their_Currency_reads_it_back()
    {
        var user = await api.CreateSignedInUserAsync();

        var set = await user.Client.PutAsJsonAsync(SettingsEndpoint + "/currency", new { currency = "USD" });

        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        var settings = await user.Client.GetFromJsonAsync<Settings>(SettingsEndpoint);
        Assert.Equal("USD", settings?.Currency);
    }

    [Fact]
    public async Task A_User_who_picked_the_wrong_Currency_can_change_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");

        var change = await user.Client.PutAsJsonAsync(SettingsEndpoint + "/currency", new { currency = "mxn" });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal("MXN", (await change.Content.ReadFromJsonAsync<Settings>())?.Currency);
        Assert.Equal("MXN", (await user.Client.GetFromJsonAsync<Settings>(SettingsEndpoint))?.Currency);
    }

    [Theory]
    [InlineData("ZZZ")]
    [InlineData("US")]
    [InlineData("XAU")]
    public async Task A_code_that_isnt_an_ISO_4217_Currency_is_rejected_and_changes_nothing(string code)
    {
        var user = await api.CreateSignedInUserAsync();

        var set = await user.Client.PutAsJsonAsync(SettingsEndpoint + "/currency", new { currency = code });

        Assert.Equal(HttpStatusCode.BadRequest, set.StatusCode);
        Assert.Equal("application/problem+json", set.Content.Headers.ContentType?.MediaType);
        var settings = await user.Client.GetFromJsonAsync<Settings>(SettingsEndpoint);
        Assert.Null(settings?.Currency);
    }

    private sealed record Settings(string Email, string? Currency);
}
