using LedgerFlow.Core.Domain;

namespace LedgerFlow.Core.Matching;

/// <summary>
/// Lets the matcher ask "have I already posted this invoice?" without knowing about the database.
/// Kept as a narrow port so <see cref="ThreeWayMatcher"/> stays pure and testable.
/// </summary>
public interface IInvoiceHistory
{
    /// <summary>True if an invoice with the same <see cref="Invoice.DeduplicationKey"/> has already been accepted.</summary>
    bool HasSeen(string deduplicationKey);
}

/// <summary>Trivial history that has seen nothing — the default for a single-invoice evaluation.</summary>
public sealed class EmptyInvoiceHistory : IInvoiceHistory
{
    public static EmptyInvoiceHistory Instance { get; } = new();
    public bool HasSeen(string deduplicationKey) => false;
}
