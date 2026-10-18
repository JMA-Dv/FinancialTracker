using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.UserSettings;

[ApiController]
[Route("api/user-settings")]
public class UserSettingsController : ControllerBase
{
    [HttpGet]
    public ActionResult<UserSettingsResponse> Get()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("A signed-in User always carries an email claim.");
        return new UserSettingsResponse(email);
    }
}

public sealed record UserSettingsResponse(string Email);
