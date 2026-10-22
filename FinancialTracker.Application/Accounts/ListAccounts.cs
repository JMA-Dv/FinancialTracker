namespace FinancialTracker.Application.Accounts;

public sealed class ListAccounts(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<IReadOnlyList<AccountSummary>> ExecuteAsync(Guid userId) =>
        await summarizer.SummarizeAsync(userId, await accounts.ListAsync(userId));
}
