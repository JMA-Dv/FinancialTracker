namespace FinancialTracker.Application.Accounts;

/// <summary>Closes an Account the User no longer uses, which is only allowed at a zero balance.</summary>
public sealed class CloseAccount(IAccountStore accounts, AccountSummarizer summarizer)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        var summary = await summarizer.SummarizeAsync(userId, account);

        account.Close(summary.Balance);
        await accounts.SaveChangesAsync();
        return summary with { IsClosed = true };
    }
}
