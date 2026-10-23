using FinancialTracker.Domain.Common;

namespace FinancialTracker.Application.Accounts;

/// <summary>Works out what an Account held at the end of a past date.</summary>
public sealed class GetAccountBalance(IAccountStore accounts, AccountSummarizer summarizer, TimeProvider clock)
{
    /// <exception cref="DomainValidationException">The date is after today.</exception>
    public async Task<decimal> ExecuteAsync(Guid userId, Guid accountId, DateOnly date)
    {
        var account = await accounts.GetAsync(userId, accountId);

        var today = clock.Today();
        if (date > today)
        {
            throw new DomainValidationException("Date in the future", $"{date:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}).");
        }

        return await summarizer.BalanceOnAsync(userId, account, date);
    }
}
