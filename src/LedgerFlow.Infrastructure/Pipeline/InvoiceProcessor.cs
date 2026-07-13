using LedgerFlow.Core.Domain;
using LedgerFlow.Core.Matching;
using LedgerFlow.Infrastructure.Erp;
using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Infrastructure.Pipeline;

/// <summary>
/// The extract → match → (post | queue) step, orchestrated in one place so the Functions host is a
/// thin trigger. Given an already-extracted invoice plus its PO and receipts, it runs the
/// <see cref="ThreeWayMatcher"/>, and either posts a clean invoice to the ERP or persists it to the
/// exception queue. Every processed invoice is recorded so duplicates are caught next time.
/// </summary>
public sealed class InvoiceProcessor
{
    private readonly ThreeWayMatcher _matcher;
    private readonly LedgerFlowDbContext _db;
    private readonly IErpPostingClient _erp;
    private readonly ILogger<InvoiceProcessor> _logger;

    public InvoiceProcessor(
        ThreeWayMatcher matcher,
        LedgerFlowDbContext db,
        IErpPostingClient erp,
        ILogger<InvoiceProcessor> logger)
    {
        _matcher = matcher;
        _db = db;
        _erp = erp;
        _logger = logger;
    }

    public async Task<MatchResult> ProcessAsync(
        Invoice invoice,
        PurchaseOrder? purchaseOrder,
        IReadOnlyCollection<GoodsReceipt> receipts,
        CancellationToken cancellationToken = default)
    {
        var result = _matcher.Match(invoice, purchaseOrder, receipts, _db);

        var record = new InvoiceRecord
        {
            InvoiceNumber = invoice.InvoiceNumber,
            SupplierId = invoice.SupplierId,
            DeduplicationKey = invoice.DeduplicationKey,
            Currency = invoice.Total.Currency,
            TotalAmount = invoice.Total.Amount,
            ExtractionConfidence = invoice.ExtractionConfidence,
            Exceptions = result.Exceptions
                .Select(e => new ExceptionRecord { Code = e.Code, Message = e.Message, Sku = e.Sku })
                .ToList(),
        };

        if (result.Decision == MatchDecision.AutoPost)
        {
            var posting = await _erp.PostAsync(invoice, cancellationToken).ConfigureAwait(false);
            record.Status = posting.Accepted ? InvoiceStatus.Posted : InvoiceStatus.NeedsReview;
            record.ResolvedAt = posting.Accepted ? DateTimeOffset.UtcNow : null;
            _logger.LogInformation("Invoice {InvoiceNumber} auto-posted to ERP.", invoice.InvoiceNumber);
        }
        else
        {
            record.Status = InvoiceStatus.NeedsReview;
            _logger.LogInformation(
                "Invoice {InvoiceNumber} routed to review with {Count} exception(s).",
                invoice.InvoiceNumber, result.Exceptions.Count);
        }

        _db.Invoices.Add(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }
}
