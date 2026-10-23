namespace FinancialTracker.Application.Accounts;

/// <summary>Lists an Account's Balance Adjustments, newest first.</summary>
public sealed class ListBalanceAdjustments(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<IReadOnlyList<BalanceAdjustmentSummary>> ExecuteAsync(Guid userId, Guid accountId) =>
        await summarizer.SummarizeAdjustmentsAsync(userId, await accounts.GetAsync(userId, accountId));
}
