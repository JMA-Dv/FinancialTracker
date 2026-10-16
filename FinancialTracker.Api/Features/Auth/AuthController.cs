using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    GoogleUsers googleUsers)
    : ControllerBase
{
    /// <summary>Sends the browser to Google. Google is the only way to sign in (ADR 0002).</summary>
    [HttpGet("google/sign-in")]
    [AllowAnonymous]
    public IActionResult SignInWithGoogle([FromQuery] string? returnUrl)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), new { returnUrl = LocalOrRoot(returnUrl) });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme, callbackUrl);

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>Where the browser lands after Google: signs the User in, creating them the first time.</summary>
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl)
    {
        var googleLogin = await signInManager.GetExternalLoginInfoAsync();
        if (googleLogin is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Google sign-in failed",
                detail: "Start again from Google sign-in.");
        }

        var user = await googleUsers.FindOrCreateAsync(googleLogin);
        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Google sign-in failed",
                detail: "Google didn't share an email address for this account.");
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        await signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: googleLogin.LoginProvider);

        return LocalRedirect(LocalOrRoot(returnUrl));
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOutUser()
    {
        // Rotating the security stamp invalidates every cookie issued to this User,
        // including copies that outlive the browser's deleted one.
        var user = await userManager.GetUserAsync(User);
        if (user is not null)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        await signInManager.SignOutAsync();
        return NoContent();
    }

    // Only ever return to this site, so sign-in can't be used as an open redirect.
    private string LocalOrRoot(string? returnUrl) =>
        returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
