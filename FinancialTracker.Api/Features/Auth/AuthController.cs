using System.ComponentModel.DataAnnotations;
using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
    : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp(SignUpRequest request)
    {
        var user = new User { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn(SignInRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            // Same answer for an unknown email and a wrong password, so emails can't be probed.
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in failed",
                detail: "The email or password is incorrect.");
        }

        return NoContent();
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
}

public sealed record SignUpRequest([Required, EmailAddress] string Email, [Required] string Password);

public sealed record SignInRequest([Required] string Email, [Required] string Password);
