using FinancialTracker.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace FinancialTracker.Api.ErrorHandling;

/// <summary>Answers a broken domain rule with a ProblemDetails body instead of a 500.</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException broken)
        {
            return false;
        }

        var status = broken is DomainConflictException ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = broken.Title, Detail = broken.Message },
        });
    }
}
