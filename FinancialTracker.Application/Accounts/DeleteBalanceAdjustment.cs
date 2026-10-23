namespace FinancialTracker.Application.Accounts;

/// <summary>Removes a Balance Adjustment, the Opening Balance included. With none left, the Account starts at zero again.</summary>
public sealed class DeleteBalanceAdjustment(IAccountStore accounts)
{
    public async Task ExecuteAsync(Guid userId, Guid accountId, Guid balanceAdjustmentId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        account.RemoveBalanceAdjustment(balanceAdjustmentId);
        await accounts.SaveChangesAsync();
    }
}
