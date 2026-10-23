namespace FinancialTracker.Application.Accounts;

/// <summary>Opens a Closed Account again so money can be recorded against it.</summary>
public sealed class ReopenAccount(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        account.Reopen();
        await accounts.SaveChangesAsync();
        return await summarizer.SummarizeAsync(userId, account);
    }
}
