using LedgerFlow.Core.Domain;

namespace LedgerFlow.Core.Matching;

/// <summary>
/// The three-way match: reconcile an <see cref="Invoice"/> against its <see cref="PurchaseOrder"/>
/// (what we agreed to buy) and its <see cref="GoodsReceipt"/>s (what actually arrived). Anything
/// inside <see cref="MatchTolerances"/> auto-posts; everything else becomes a typed exception for
/// a human. The method collects <em>all</em> problems rather than failing on the first, because a
/// reviewer wants the whole picture in one pass.
/// </summary>
public sealed class ThreeWayMatcher
{
    private readonly MatchTolerances _tolerances;

    public ThreeWayMatcher(MatchTolerances? tolerances = null) =>
        _tolerances = tolerances ?? MatchTolerances.Default;

    public MatchResult Match(
        Invoice invoice,
        PurchaseOrder? purchaseOrder,
        IReadOnlyCollection<GoodsReceipt> receipts,
        IInvoiceHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(receipts);
        history ??= EmptyInvoiceHistory.Instance;

        var exceptions = new List<MatchException>();

        if (invoice.ExtractionConfidence < _tolerances.MinExtractionConfidence)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.LowConfidence,
                $"Extraction confidence {invoice.ExtractionConfidence:P0} is below the " +
                $"{_tolerances.MinExtractionConfidence:P0} threshold."));
        }

        if (history.HasSeen(invoice.DeduplicationKey))
        {
            exceptions.Add(new MatchException(
                ExceptionCode.DuplicateInvoice,
                $"Invoice {invoice.InvoiceNumber} from supplier {invoice.SupplierId} has already been posted."));
        }

        // Without a PO there is nothing to match against; stop here — the per-line checks below
        // would all be noise on top of the real problem.
        if (purchaseOrder is null)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.MissingPurchaseOrder,
                invoice.PurchaseOrderNumber is null
                    ? "Invoice carries no PO number."
                    : $"No open purchase order {invoice.PurchaseOrderNumber} was found."));
            return MatchResult.NeedsReview(exceptions);
        }

        if (!string.Equals(invoice.SupplierId, purchaseOrder.SupplierId, StringComparison.OrdinalIgnoreCase))
        {
            exceptions.Add(new MatchException(
                ExceptionCode.SupplierMismatch,
                $"Invoice supplier {invoice.SupplierId} does not match PO supplier {purchaseOrder.SupplierId}."));
        }

        if (!string.Equals(invoice.Total.Currency, purchaseOrder.Currency, StringComparison.OrdinalIgnoreCase))
        {
            // A currency mismatch makes every downstream amount comparison meaningless, so return early.
            exceptions.Add(new MatchException(
                ExceptionCode.CurrencyMismatch,
                $"Invoice currency {invoice.Total.Currency} does not match PO currency {purchaseOrder.Currency}."));
            return MatchResult.NeedsReview(exceptions);
        }

        var receivedBySku = SumReceiptsBySku(receipts, purchaseOrder.PurchaseOrderNumber);

        foreach (var line in invoice.Lines)
        {
            var poLine = purchaseOrder.FindLine(line.Sku);
            if (poLine is null)
            {
                exceptions.Add(new MatchException(
                    ExceptionCode.UnorderedItem,
                    $"SKU {line.Sku} is on the invoice but not on the purchase order.",
                    line.Sku));
                continue;
            }

            CheckPrice(line, poLine, exceptions);
            CheckQuantityAgainstReceipts(line, receivedBySku, exceptions);
        }

        CheckTotals(invoice, exceptions);

        return exceptions.Count == 0
            ? MatchResult.Clean()
            : MatchResult.NeedsReview(exceptions);
    }

    private void CheckPrice(InvoiceLine line, PurchaseOrderLine poLine, List<MatchException> exceptions)
    {
        var invoicePrice = line.UnitPrice.Amount;
        var poPrice = poLine.UnitPrice.Amount;
        var allowed = poPrice * _tolerances.UnitPriceRelativeTolerance + _tolerances.UnitPriceAbsoluteTolerance;

        if (Math.Abs(invoicePrice - poPrice) > allowed)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.PriceOverTolerance,
                $"SKU {line.Sku}: invoiced {line.UnitPrice} vs PO {poLine.UnitPrice} exceeds tolerance.",
                line.Sku));
        }
    }

    private void CheckQuantityAgainstReceipts(
        InvoiceLine line,
        IReadOnlyDictionary<string, decimal> receivedBySku,
        List<MatchException> exceptions)
    {
        if (!receivedBySku.TryGetValue(Normalise(line.Sku), out var received) || received <= 0m)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.NoGoodsReceipt,
                $"SKU {line.Sku}: invoiced {line.Quantity} but nothing has been received yet.",
                line.Sku));
            return;
        }

        var allowed = received * (1m + _tolerances.QuantityOverReceiptTolerance);
        if (line.Quantity > allowed)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.QuantityOverReceipt,
                $"SKU {line.Sku}: invoiced {line.Quantity} exceeds received {received}.",
                line.Sku));
        }
    }

    private static void CheckTotals(Invoice invoice, List<MatchException> exceptions)
    {
        var lineSum = invoice.Lines.Aggregate(
            Money.Zero(invoice.Total.Currency),
            (acc, l) => acc + l.LineTotal);

        var expected = invoice.Tax is { } tax ? lineSum + tax : lineSum;

        // Allow a cent of rounding drift between summed lines (+ tax) and the stated total.
        if (Money.AbsoluteDifference(expected, invoice.Total).Amount > 0.01m)
        {
            exceptions.Add(new MatchException(
                ExceptionCode.TotalsMismatch,
                $"Line items{(invoice.Tax is null ? "" : " + tax")} total {expected} but the invoice states {invoice.Total}."));
        }
    }

    private static IReadOnlyDictionary<string, decimal> SumReceiptsBySku(
        IEnumerable<GoodsReceipt> receipts,
        string purchaseOrderNumber)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var receipt in receipts)
        {
            if (!string.Equals(receipt.PurchaseOrderNumber, purchaseOrderNumber, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var line in receipt.Lines)
            {
                var key = Normalise(line.Sku);
                totals[key] = totals.GetValueOrDefault(key) + line.ReceivedQuantity;
            }
        }

        return totals;
    }

    private static string Normalise(string sku) => sku.Trim().ToUpperInvariant();
}
