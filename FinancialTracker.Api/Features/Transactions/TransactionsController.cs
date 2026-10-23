using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Transactions;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Payees;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Transactions;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(
    CreateTransaction createTransaction,
    ListTransactions listTransactions,
    EditTransaction editTransaction,
    DeleteTransaction deleteTransaction,
    IValidator<SaveTransactionRequest> saveValidator)
    : ControllerBase
{
    /// <summary>The User's Transactions, newest first. Every filter is optional and they combine.</summary>
    /// <param name="from">The earliest date to include.</param>
    /// <param name="payeeId">Only Transactions with this Payee.</param>
    /// <param name="to">The latest date to include.</param>
    [HttpGet]
    public async Task<IReadOnlyList<TransactionResponse>> List(Guid? accountId, Guid? categoryId, Guid? payeeId, DateOnly? from, DateOnly? to) =>
        (await listTransactions.ExecuteAsync(User.Id(), new TransactionFilter(accountId, categoryId, payeeId, from, to)))
            .Select(TransactionResponse.Of)
            .ToList();

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(SaveTransactionRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        var transaction = await createTransaction.ExecuteAsync(User.Id(), request.ToDetails());
        return Created($"/api/transactions/{transaction.Id}", TransactionResponse.Of(transaction));
    }

    /// <summary>Replaces every field. The Transaction can move to another Account only if it has the same Currency.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Edit(Guid id, SaveTransactionRequest request)
    {
        if (await InvalidAsync(request) is { } invalid)
        {
            return invalid;
        }

        return TransactionResponse.Of(await editTransaction.ExecuteAsync(User.Id(), id, request.ToDetails()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid id)
    {
        await deleteTransaction.ExecuteAsync(User.Id(), id);
        return NoContent();
    }

    /// <summary>A 400 listing every broken rule, or null when the request is valid and can go on to the use case.</summary>
    private async Task<ActionResult?> InvalidAsync(SaveTransactionRequest request)
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

internal sealed class SaveTransactionRequestValidator : AbstractValidator<SaveTransactionRequest>
{
    public SaveTransactionRequestValidator()
    {
        RuleFor(r => r.AccountId).NotNull();
        RuleFor(r => r.Date).NotNull();
        RuleFor(r => r.Direction).NotNull().IsInEnum();
        RuleFor(r => r.Amount).NotNull();
        RuleFor(r => r.CategoryId).NotNull();
        RuleFor(r => r.PayeeName).MaximumLength(Payee.MaxNameLength);
    }
}

/// <param name="Amount">Positive, in the Account's Currency. The direction says which way it moves.</param>
/// <param name="PayeeName">Who was paid or paid the User. An unknown name creates the Payee; leave it out for none.</param>
public sealed record SaveTransactionRequest(
    Guid? AccountId, DateOnly? Date, TransactionDirection? Direction, decimal? Amount, Guid? CategoryId, string? PayeeName, string? Note)
{
    public TransactionDetails ToDetails() =>
        new(
            AccountId ?? throw new InvalidOperationException("AccountId is checked by the validator."),
            Date ?? throw new InvalidOperationException("Date is checked by the validator."),
            Direction ?? throw new InvalidOperationException("Direction is checked by the validator."),
            Amount ?? throw new InvalidOperationException("Amount is checked by the validator."),
            CategoryId ?? throw new InvalidOperationException("CategoryId is checked by the validator."),
            PayeeName,
            Note);
}

public sealed record TransactionResponse(
    Guid Id, Guid AccountId, DateOnly Date, TransactionDirection Direction, decimal Amount, Guid CategoryId, Guid? PayeeId, string? Note)
{
    public static TransactionResponse Of(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.Date,
            transaction.Direction,
            transaction.Amount,
            transaction.CategoryId,
            transaction.PayeeId,
            transaction.Note);
}
