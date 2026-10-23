namespace FinancialTracker.Application.Accounts;

/// <summary>Replaces the date and stated balance of one of the Account's Balance Adjustments, the Opening Balance included.</summary>
public sealed class EditBalanceAdjustment(IAccountStore accounts, AccountSummarizer summarizer, TimeProvider clock)
{
    public async Task<BalanceAdjustmentSummary> ExecuteAsync(
        Guid userId, Guid accountId, Guid balanceAdjustmentId, BalanceAdjustmentDetails details)
    {
        var account = await accounts.GetAsync(userId, accountId);
        var adjustment = account.EditBalanceAdjustment(balanceAdjustmentId, details.Date, details.Balance, clock.Today());
        await accounts.SaveChangesAsync();
        return await summarizer.SummarizeAsync(userId, account, adjustment);
    }
}
