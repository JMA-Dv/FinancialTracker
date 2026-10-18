namespace FinancialTracker.Application;

public static class TimeProviderExtensions
{
    /// <summary>Today's date in UTC, the date future-dated money is checked against.</summary>
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
