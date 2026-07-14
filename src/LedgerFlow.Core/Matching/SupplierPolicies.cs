namespace LedgerFlow.Core.Matching;

/// <summary>
/// Per-supplier tolerance overrides. Finance teams trust suppliers unevenly: a strategic partner
/// with clean invoices can carry a looser price band, while a supplier with a history of
/// over-billing gets zero headroom. Unknown suppliers fall back to the default policy.
/// </summary>
public sealed class SupplierPolicies
{
    private readonly MatchTolerances _default;
    private readonly IReadOnlyDictionary<string, MatchTolerances> _overrides;

    public SupplierPolicies(
        MatchTolerances? defaultTolerances = null,
        IReadOnlyDictionary<string, MatchTolerances>? overrides = null)
    {
        _default = defaultTolerances ?? MatchTolerances.Default;
        _overrides = overrides is null
            ? new Dictionary<string, MatchTolerances>()
            : new Dictionary<string, MatchTolerances>(overrides, StringComparer.OrdinalIgnoreCase);
    }

    public MatchTolerances For(string supplierId) =>
        _overrides.TryGetValue(supplierId.Trim(), out var specific) ? specific : _default;

    /// <summary>A matcher configured for the given supplier.</summary>
    public ThreeWayMatcher MatcherFor(string supplierId) => new(For(supplierId));
}
