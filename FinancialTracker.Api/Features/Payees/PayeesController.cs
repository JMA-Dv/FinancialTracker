using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Payees;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Payees;

[ApiController]
[Route("api/payees")]
public class PayeesController(ListPayees listPayees, RenamePayee renamePayee, IValidator<RenamePayeeRequest> renameValidator)
    : ControllerBase
{
    /// <summary>The Payees by name. Payees are created by naming them on a Transaction.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<PayeeResponse>> List() =>
        (await listPayees.ExecuteAsync(User.Id())).Select(PayeeResponse.Of).ToList();

    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<PayeeResponse>> Rename(Guid id, RenamePayeeRequest request)
    {
        var result = await renameValidator.ValidateAsync(request, HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        return PayeeResponse.Of(await renamePayee.ExecuteAsync(User.Id(), id, request.Name));
    }
}

internal sealed class RenamePayeeRequestValidator : AbstractValidator<RenamePayeeRequest>
{
    public RenamePayeeRequestValidator() => RuleFor(r => r.Name).NotNull().MaximumLength(Payee.MaxNameLength);
}

public sealed record RenamePayeeRequest(string Name);

/// <param name="SuggestedCategoryId">The Category of the most recent Transaction with the Payee, or null if there is none.</param>
public sealed record PayeeResponse(Guid Id, string Name, Guid? SuggestedCategoryId)
{
    public static PayeeResponse Of(PayeeSummary summary) => new(summary.Payee.Id, summary.Payee.Name, summary.SuggestedCategoryId);
}
