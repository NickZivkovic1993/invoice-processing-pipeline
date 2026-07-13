namespace LedgerFlow.Core.Domain;

/// <summary>
/// Proof that goods actually arrived. A PO line can be received in several deliveries,
/// so the matcher works against the sum of receipts rather than any single one.
/// </summary>
public sealed record GoodsReceipt
{
    public required string ReceiptNumber { get; init; }
    public required string PurchaseOrderNumber { get; init; }
    public required DateOnly ReceivedOn { get; init; }
    public required IReadOnlyList<GoodsReceiptLine> Lines { get; init; }
}

public sealed record GoodsReceiptLine
{
    public required string Sku { get; init; }
    public required decimal ReceivedQuantity { get; init; }
}
