namespace FinancialTracker.Domain.Common;

/// <summary>Input that breaks a domain rule, such as an unknown Currency code.</summary>
public class DomainValidationException(string title, string detail) : DomainException(title, detail);
