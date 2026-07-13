using LedgerFlow.Core.Domain;

namespace LedgerFlow.Infrastructure.Erp;

/// <summary>
/// Supplies the "two other sides" of the three-way match — the purchase order and the goods
/// receipts — from the ERP / procurement system, keyed by the PO number on the invoice.
/// </summary>
public interface IReferenceDataProvider
{
    Task<PurchaseOrder?> GetPurchaseOrderAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GoodsReceipt>> GetReceiptsAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default);
}
