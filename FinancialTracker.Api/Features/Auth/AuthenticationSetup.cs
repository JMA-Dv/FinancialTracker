using FinancialTracker.Infrastructure.Identity;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FinancialTracker.Api.Features.Auth;

public static class AuthenticationSetup
{
    public const string GoogleSectionName = "Authentication:Google";

    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        // Google is the only way to sign in (ADR 0002). It lands in Identity's external cookie,
        // and the callback turns that into the application cookie.
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;

                // Declined consent, or a broken round trip (wrong client secret, sign-in over plain http).
                // Log why, so a misconfiguration can be told apart from a visitor saying no.
                options.Events.OnRemoteFailure = async context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(AuthenticationSetup));
                    logger.LogWarning(context.Failure, "Google sign-in failed on the way back from Google.");

                    context.HandleResponse();
                    await Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Google sign-in failed",
                            detail: "Google sign-in was cancelled or didn't complete. Start again from Google sign-in.")
                        .ExecuteAsync(context.HttpContext);
                };
            })
            .AddIdentityCookies();

        // The client id and secret come from user secrets or environment variables, never appsettings.json.
        // Validating on start makes a missing one stop the app at startup rather than at the first sign-in.
        services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
            .BindConfiguration(GoogleSectionName)
            .ValidateOnStart();

        // Users are identified by their Google account id; the email is display-only and not unique.
        services.AddIdentityCore<User>()
            .AddSignInManager()
            .AddEntityFrameworkStores<FinanceDbContext>();

        services.AddScoped<GoogleUsers>();

        // Check the security stamp on every request so a signed-out cookie stops working immediately.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // This is an API: answer with status codes instead of redirecting to a login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Every endpoint requires a signed-in User unless it explicitly opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
