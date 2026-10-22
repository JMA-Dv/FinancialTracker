namespace FinancialTracker.Application.Accounts;

public sealed class GetAccount(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId) =>
        await summarizer.SummarizeAsync(userId, await accounts.GetAsync(userId, accountId));
}
