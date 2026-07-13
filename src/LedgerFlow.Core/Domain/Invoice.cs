namespace LedgerFlow.Core.Domain;

/// <summary>
/// A supplier invoice as extracted from a document. Field-level confidence is carried
/// alongside the value because the extractor is probabilistic and low-confidence fields
/// are a legitimate reason to route to human review.
/// </summary>
public sealed record Invoice
{
    public required string InvoiceNumber { get; init; }
    public required string SupplierId { get; init; }
    public required DateOnly InvoiceDate { get; init; }

    /// <summary>PO number as printed on the invoice. Null when the supplier omitted it.</summary>
    public string? PurchaseOrderNumber { get; init; }

    public required Money Total { get; init; }
    public Money? Tax { get; init; }

    public required IReadOnlyList<InvoiceLine> Lines { get; init; }

    /// <summary>Lowest field-level confidence reported by the extractor, in [0, 1].</summary>
    public double ExtractionConfidence { get; init; } = 1.0;

    /// <summary>
    /// Natural key used to spot the same invoice arriving twice (re-sent email, duplicate upload).
    /// Suppliers reuse invoice numbers across years, so the date is not part of the key by design —
    /// a supplier re-issuing the same number is itself something a human should look at.
    /// </summary>
    public string DeduplicationKey =>
        $"{SupplierId.Trim().ToUpperInvariant()}|{InvoiceNumber.Trim().ToUpperInvariant()}";
}

public sealed record InvoiceLine
{
    public required string Sku { get; init; }
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required Money UnitPrice { get; init; }

    public Money LineTotal => UnitPrice * Quantity;
}
