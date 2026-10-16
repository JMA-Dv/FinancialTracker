using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialTracker.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for the round trip to Google. On challenge, the Google account named in the request
/// headers "consents": its claims land in Identity's external cookie and the browser is sent to
/// the app's callback, which is where the real Google handler leaves things too.
/// </summary>
public sealed class FakeGoogleHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SubjectHeader = "X-Test-Google-Subject";
    public const string EmailHeader = "X-Test-Google-Email";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var claims = new List<Claim>();
        if (Request.Headers.TryGetValue(SubjectHeader, out var subject))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, subject.ToString()));
        }

        if (Request.Headers.TryGetValue(EmailHeader, out var email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        }

        var googleAccount = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        await Context.SignInAsync(IdentityConstants.ExternalScheme, googleAccount, properties);
        Response.Redirect(properties.RedirectUri ?? "/");
    }
}
