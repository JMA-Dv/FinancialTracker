namespace FinancialTracker.Domain.Common;

/// <summary>
/// Something the User asked for doesn't exist, or belongs to another User: the two are never told apart,
/// so identifiers can't be probed.
/// </summary>
public class DomainNotFoundException(string title, string detail) : DomainException(title, detail);
