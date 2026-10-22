using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Transfers;
using FinancialTracker.Domain.Transfers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Transfers;

[ApiController]
[Route("api/transfers")]
public class TransfersController(
    CreateTransfer createTransfer,
    GetTransfer getTransfer,
    ListTransfers listTransfers,
    EditTransfer editTransfer,
    DeleteTransfer deleteTransfer,
    IValidator<SaveTransferRequest> saveValidator)
    : ControllerBase
{
    /// <summary>The User's Transfers, newest first. With an Account, only those into or out of it.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<TransferResponse>> List(Guid? accountId) =>
        (await listTransfers.ExecuteAsync(User.Id(), accountId)).Select(TransferResponse.Of).ToList();

    [HttpGet("{id:guid}")]
    public async Task<TransferResponse> Get(Guid id) => TransferResponse.Of(await getTransfer.ExecuteAsync(User.Id(), id));

    [HttpPost]
    public async Task<ActionResult<TransferResponse>> Create(SaveTransferRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        var transfer = await createTransfer.ExecuteAsync(User.Id(), request.ToDetails());
        return CreatedAtAction(nameof(Get), new { id = transfer.Id }, TransferResponse.Of(transfer));
    }

    /// <summary>Replaces every field.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransferResponse>> Edit(Guid id, SaveTransferRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        return TransferResponse.Of(await editTransfer.ExecuteAsync(User.Id(), id, request.ToDetails()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid id)
    {
        await deleteTransfer.ExecuteAsync(User.Id(), id);
        return NoContent();
    }

    /// <summary>A 400 listing every broken rule, or null when the request is valid and can go on to the use case.</summary>
    private async Task<ActionResult?> InvalidAsync(SaveTransferRequest request)
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

internal sealed class SaveTransferRequestValidator : AbstractValidator<SaveTransferRequest>
{
    public SaveTransferRequestValidator()
    {
        RuleFor(r => r.FromAccountId).NotNull();
        RuleFor(r => r.ToAccountId).NotNull();
        RuleFor(r => r.Date).NotNull();
        RuleFor(r => r.Amount).NotNull();
    }
}

/// <param name="Amount">Positive, what leaves the from-Account, in its Currency.</param>
/// <param name="ReceivedAmount">What arrives in the to-Account, in its Currency. Required between Currencies, optional otherwise.</param>
public sealed record SaveTransferRequest(
    Guid? FromAccountId, Guid? ToAccountId, DateOnly? Date, decimal? Amount, decimal? ReceivedAmount, string? Note)
{
    public TransferDetails ToDetails() =>
        new(
            FromAccountId ?? throw new InvalidOperationException("FromAccountId is checked by the validator."),
            ToAccountId ?? throw new InvalidOperationException("ToAccountId is checked by the validator."),
            Date ?? throw new InvalidOperationException("Date is checked by the validator."),
            Amount ?? throw new InvalidOperationException("Amount is checked by the validator."),
            ReceivedAmount,
            Note);
}

public sealed record TransferResponse(
    Guid Id, Guid FromAccountId, Guid ToAccountId, DateOnly Date, decimal Amount, decimal ReceivedAmount, string? Note)
{
    public static TransferResponse Of(Transfer transfer) =>
        new(transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Date, transfer.AmountSent, transfer.AmountReceived, transfer.Note);
}
