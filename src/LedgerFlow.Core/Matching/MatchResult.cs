namespace LedgerFlow.Core.Matching;

public enum MatchDecision
{
    /// <summary>Everything reconciled within tolerance — safe to post to the ERP automatically.</summary>
    AutoPost,

    /// <summary>At least one discrepancy needs a human. See <see cref="MatchResult.Exceptions"/>.</summary>
    Review,
}

public enum ExceptionCode
{
    LowConfidence,
    DuplicateInvoice,
    MissingPurchaseOrder,
    SupplierMismatch,
    CurrencyMismatch,
    UnorderedItem,
    PriceOverTolerance,
    QuantityOverReceipt,
    NoGoodsReceipt,
    TotalsMismatch,
}

/// <summary>One thing that stopped an invoice from auto-posting, in human-readable terms.</summary>
public sealed record MatchException(ExceptionCode Code, string Message, string? Sku = null);

public sealed record MatchResult
{
    public required MatchDecision Decision { get; init; }
    public required IReadOnlyList<MatchException> Exceptions { get; init; }

    public bool IsClean => Exceptions.Count == 0;

    public static MatchResult Clean() =>
        new() { Decision = MatchDecision.AutoPost, Exceptions = Array.Empty<MatchException>() };

    public static MatchResult NeedsReview(IReadOnlyList<MatchException> exceptions) =>
        new() { Decision = MatchDecision.Review, Exceptions = exceptions };
}
