namespace FinancialTracker.Application.Accounts;

/// <summary>States an Account's real balance at the end of a date, which also adds an Opening Balance to an Account without one.</summary>
public sealed class CreateBalanceAdjustment(IAccountStore accounts, AccountSummarizer summarizer, TimeProvider clock)
{
    public async Task<BalanceAdjustmentSummary> ExecuteAsync(Guid userId, Guid accountId, BalanceAdjustmentDetails details)
    {
        var account = await accounts.GetAsync(userId, accountId);
        var adjustment = account.AdjustBalance(details.Date, details.Balance, clock.Today());
        await accounts.SaveChangesAsync();
        return await summarizer.SummarizeAsync(userId, account, adjustment);
    }
}
