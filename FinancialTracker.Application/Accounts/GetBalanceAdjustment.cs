namespace FinancialTracker.Application.Accounts;

public sealed class GetBalanceAdjustment(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<BalanceAdjustmentSummary> ExecuteAsync(Guid userId, Guid accountId, Guid balanceAdjustmentId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        return await summarizer.SummarizeAsync(userId, account, account.GetBalanceAdjustment(balanceAdjustmentId));
    }
}
