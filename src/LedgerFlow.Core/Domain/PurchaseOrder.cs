namespace LedgerFlow.Core.Domain;

/// <summary>An open purchase order from the ERP — the contractual truth about price and quantity.</summary>
public sealed record PurchaseOrder
{
    public required string PurchaseOrderNumber { get; init; }
    public required string SupplierId { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyList<PurchaseOrderLine> Lines { get; init; }

    public PurchaseOrderLine? FindLine(string sku) =>
        Lines.FirstOrDefault(l => string.Equals(l.Sku, sku, StringComparison.OrdinalIgnoreCase));
}

public sealed record PurchaseOrderLine
{
    public required string Sku { get; init; }
    public required decimal OrderedQuantity { get; init; }
    public required Money UnitPrice { get; init; }
}
