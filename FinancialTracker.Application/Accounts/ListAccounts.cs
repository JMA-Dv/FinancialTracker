namespace FinancialTracker.Application.Accounts;

/// <summary>Lists the User's Accounts in the order they were created. Closed Accounts are left out unless asked for.</summary>
public sealed class ListAccounts(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<IReadOnlyList<AccountSummary>> ExecuteAsync(Guid userId, bool includeClosed) =>
        await summarizer.SummarizeAsync(
            userId, (await accounts.ListAsync(userId)).Where(account => includeClosed || !account.IsClosed).ToList());
}
