namespace LedgerFlow.Core.Matching;

/// <summary>
/// Tolerances that decide when a discrepancy is small enough to auto-post. These are the knobs
/// a finance team actually tunes: how much price drift to absorb, whether short/over deliveries
/// are acceptable, and how sure the extractor must be before a value is trusted.
/// </summary>
public sealed record MatchTolerances
{
    /// <summary>Relative unit-price tolerance, e.g. 0.02 = allow the invoice price to differ from the PO by 2%.</summary>
    public decimal UnitPriceRelativeTolerance { get; init; } = 0.02m;

    /// <summary>Absolute per-unit tolerance, applied in addition to the relative one to avoid tripping on rounding of cheap items.</summary>
    public decimal UnitPriceAbsoluteTolerance { get; init; } = 0.01m;

    /// <summary>Quantity invoiced may exceed quantity received by this fraction (0 = never over-bill).</summary>
    public decimal QuantityOverReceiptTolerance { get; init; } = 0m;

    /// <summary>Reject any invoice whose lowest field confidence is below this. Below it, a human should look.</summary>
    public double MinExtractionConfidence { get; init; } = 0.80;

    public static MatchTolerances Default { get; } = new();
}
