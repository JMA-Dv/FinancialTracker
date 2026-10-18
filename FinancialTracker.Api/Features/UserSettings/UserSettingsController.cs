using System.Security.Claims;
using FinancialTracker.Application.UserSettings;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.UserSettings;

[ApiController]
[Route("api/user-settings")]
public class UserSettingsController(GetUserSettings getUserSettings, SetCurrency setCurrency) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserSettingsResponse>> Get()
    {
        var settings = await getUserSettings.ExecuteAsync(CurrentUserId());
        return new UserSettingsResponse(settings.Email, settings.Currency?.Code);
    }

    /// <summary>Sets or changes the User's Currency, the default for new Accounts.</summary>
    [HttpPut("currency")]
    public async Task<ActionResult<UserSettingsResponse>> SetCurrency(SetCurrencyRequest request)
    {
        await setCurrency.ExecuteAsync(CurrentUserId(), request.Currency);
        return await Get();
    }

    private Guid CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("A signed-in User always carries their id.");
}

public sealed record UserSettingsResponse(string Email, string? Currency);

/// <param name="Currency">An ISO 4217 code, such as USD.</param>
public sealed record SetCurrencyRequest(string Currency);
