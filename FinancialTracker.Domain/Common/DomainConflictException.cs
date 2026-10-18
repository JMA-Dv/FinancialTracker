namespace FinancialTracker.Domain.Common;

/// <summary>A request that's fine on its own but conflicts with the User's current state.</summary>
public class DomainConflictException(string title, string detail) : DomainException(title, detail);
