using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Money;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Accounts;

[ApiController]
[Route("api/accounts/{accountId:guid}/balance-adjustments")]
public class BalanceAdjustmentsController(
    CreateBalanceAdjustment createBalanceAdjustment,
    GetBalanceAdjustment getBalanceAdjustment,
    ListBalanceAdjustments listBalanceAdjustments,
    EditBalanceAdjustment editBalanceAdjustment,
    DeleteBalanceAdjustment deleteBalanceAdjustment,
    IValidator<SaveBalanceAdjustmentRequest> saveValidator)
    : ControllerBase
{
    /// <summary>The Account's Balance Adjustments, newest first. The Opening Balance is the oldest.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<BalanceAdjustmentResponse>> List(Guid accountId) =>
        (await listBalanceAdjustments.ExecuteAsync(User.Id(), accountId)).Select(BalanceAdjustmentResponse.Of).ToList();

    /// <summary>One of the Account's Balance Adjustments, with the difference it made.</summary>
    [HttpGet("{id:guid}")]
    public async Task<BalanceAdjustmentResponse> Get(Guid accountId, Guid id) =>
        BalanceAdjustmentResponse.Of(await getBalanceAdjustment.ExecuteAsync(User.Id(), accountId, id));

    /// <summary>Refused with 409 if the Account already has a Balance Adjustment for that date.</summary>
    [HttpPost]
    public async Task<ActionResult<BalanceAdjustmentResponse>> Create(Guid accountId, SaveBalanceAdjustmentRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        var adjustment = await createBalanceAdjustment.ExecuteAsync(User.Id(), accountId, request.ToDetails());
        return CreatedAtAction(nameof(Get), new { accountId, id = adjustment.Id }, BalanceAdjustmentResponse.Of(adjustment));
    }

    /// <summary>Replaces every field. Refused with 409 if another Balance Adjustment already has that date.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BalanceAdjustmentResponse>> Edit(Guid accountId, Guid id, SaveBalanceAdjustmentRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        return BalanceAdjustmentResponse.Of(await editBalanceAdjustment.ExecuteAsync(User.Id(), accountId, id, request.ToDetails()));
    }

    /// <summary>Removes a Balance Adjustment, the Opening Balance included. With none left, the Account starts at zero again.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid accountId, Guid id)
    {
        await deleteBalanceAdjustment.ExecuteAsync(User.Id(), accountId, id);
        return NoContent();
    }

    /// <summary>A 400 listing every broken rule, or null when the request is valid and can go on to the use case.</summary>
    private async Task<ActionResult?> InvalidAsync(SaveBalanceAdjustmentRequest request)
    {
        var result = await saveValidator.ValidateAsync(request, HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return null;
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }
}

internal sealed class SaveBalanceAdjustmentRequestValidator : AbstractValidator<SaveBalanceAdjustmentRequest>
{
    public SaveBalanceAdjustmentRequestValidator()
    {
        RuleFor(r => r.Date).NotNull();
        RuleFor(r => r.Balance).NotNull().InclusiveBetween(-Currency.MaxAmount, Currency.MaxAmount);
    }
}

/// <param name="Date">The end of this date is when the balance was real. Not after today.</param>
/// <param name="Balance">The signed balance at the end of the date: negative is money owed.</param>
public sealed record SaveBalanceAdjustmentRequest(DateOnly? Date, decimal? Balance)
{
    public BalanceAdjustmentDetails ToDetails() =>
        new(
            Date ?? throw new InvalidOperationException("Date is checked by the validator."),
            Balance ?? throw new InvalidOperationException("Balance is checked by the validator."));
}

/// <summary>A Balance Adjustment as the User sees it.</summary>
/// <param name="Difference">What the Balance Adjustment made up: the stated balance minus what the tracker would otherwise have shown.</param>
public sealed record BalanceAdjustmentResponse(Guid Id, DateOnly Date, decimal Balance, decimal Difference)
{
    public static BalanceAdjustmentResponse Of(BalanceAdjustmentSummary adjustment) =>
        new(adjustment.Id, adjustment.Date, adjustment.Balance, adjustment.Difference);
}
