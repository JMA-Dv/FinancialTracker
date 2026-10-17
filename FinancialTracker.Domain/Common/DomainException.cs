namespace FinancialTracker.Domain.Common;

/// <summary>A broken domain rule. Nothing was changed.</summary>
public abstract class DomainException(string title, string detail) : Exception(detail)
{
    /// <summary>A short summary of which rule was broken, the same every time it is.</summary>
    public string Title { get; } = title;
}
