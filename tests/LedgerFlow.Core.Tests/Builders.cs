using LedgerFlow.Core.Domain;

namespace LedgerFlow.Core.Tests;

/// <summary>
/// Small builders so each test states only the field it cares about. A "happy" invoice/PO/receipt
/// trio matches cleanly; every test starts from that and perturbs one thing.
/// </summary>
internal static class Builders
{
    public const string Supplier = "SUP-001";
    public const string Po = "PO-1000";
    public const string Currency = "EUR";

    public static Invoice Invoice(
        IReadOnlyList<InvoiceLine>? lines = null,
        Money? total = null,
        Money? tax = null,
        string? supplier = null,
        string invoiceNumber = "INV-500",
        double confidence = 1.0)
    {
        lines ??= new[] { Line() };
        var computed = lines.Aggregate(Money.Zero(Currency), (a, l) => a + l.LineTotal);
        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            SupplierId = supplier ?? Supplier,
            InvoiceDate = new DateOnly(2026, 6, 1),
            PurchaseOrderNumber = Po,
            Total = total ?? (tax is { } t ? computed + t : computed),
            Tax = tax,
            Lines = lines,
            ExtractionConfidence = confidence,
        };
    }

    public static InvoiceLine Line(string sku = "SKU-1", decimal qty = 10m, decimal unitPrice = 5.00m) =>
        new()
        {
            Sku = sku,
            Description = sku,
            Quantity = qty,
            UnitPrice = new Money(unitPrice, Currency),
        };

    public static PurchaseOrder Po_(IReadOnlyList<PurchaseOrderLine>? lines = null, string? supplier = null) =>
        new()
        {
            PurchaseOrderNumber = Po,
            SupplierId = supplier ?? Supplier,
            Currency = Currency,
            Lines = lines ?? new[] { PoLine() },
        };

    public static PurchaseOrderLine PoLine(string sku = "SKU-1", decimal ordered = 10m, decimal unitPrice = 5.00m) =>
        new()
        {
            Sku = sku,
            OrderedQuantity = ordered,
            UnitPrice = new Money(unitPrice, Currency),
        };

    public static GoodsReceipt Receipt(IReadOnlyList<GoodsReceiptLine>? lines = null, string? po = null) =>
        new()
        {
            ReceiptNumber = "GR-1",
            PurchaseOrderNumber = po ?? Po,
            ReceivedOn = new DateOnly(2026, 5, 30),
            Lines = lines ?? new[] { ReceiptLine() },
        };

    public static GoodsReceiptLine ReceiptLine(string sku = "SKU-1", decimal received = 10m) =>
        new() { Sku = sku, ReceivedQuantity = received };
}
