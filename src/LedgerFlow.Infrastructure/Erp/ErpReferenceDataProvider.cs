using System.Net;
using System.Net.Http.Json;
using LedgerFlow.Core.Domain;

namespace LedgerFlow.Infrastructure.Erp;

/// <summary>Reads purchase orders and goods receipts from the ERP's read API over HTTP.</summary>
public sealed class ErpReferenceDataProvider : IReferenceDataProvider
{
    private readonly HttpClient _http;

    public ErpReferenceDataProvider(HttpClient http) => _http = http;

    public async Task<PurchaseOrder?> GetPurchaseOrderAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default)
    {
        using var response = await _http
            .GetAsync($"api/purchase-orders/{Uri.EscapeDataString(purchaseOrderNumber)}", cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return dto?.ToDomain();
    }

    public async Task<IReadOnlyCollection<GoodsReceipt>> GetReceiptsAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default)
    {
        var dtos = await _http
            .GetFromJsonAsync<List<GoodsReceiptDto>>(
                $"api/goods-receipts?po={Uri.EscapeDataString(purchaseOrderNumber)}", cancellationToken)
            .ConfigureAwait(false);

        return dtos?.Select(d => d.ToDomain()).ToList() ?? new List<GoodsReceipt>();
    }

    private sealed record PurchaseOrderDto(string Number, string SupplierId, string Currency, List<PoLineDto> Lines)
    {
        public PurchaseOrder ToDomain() => new()
        {
            PurchaseOrderNumber = Number,
            SupplierId = SupplierId,
            Currency = Currency,
            Lines = Lines.Select(l => new PurchaseOrderLine
            {
                Sku = l.Sku,
                OrderedQuantity = l.OrderedQuantity,
                UnitPrice = new Money(l.UnitPrice, Currency),
            }).ToList(),
        };
    }

    private sealed record PoLineDto(string Sku, decimal OrderedQuantity, decimal UnitPrice);

    private sealed record GoodsReceiptDto(string Number, string PurchaseOrderNumber, DateOnly ReceivedOn, List<GrLineDto> Lines)
    {
        public GoodsReceipt ToDomain() => new()
        {
            ReceiptNumber = Number,
            PurchaseOrderNumber = PurchaseOrderNumber,
            ReceivedOn = ReceivedOn,
            Lines = Lines.Select(l => new GoodsReceiptLine { Sku = l.Sku, ReceivedQuantity = l.ReceivedQuantity }).ToList(),
        };
    }

    private sealed record GrLineDto(string Sku, decimal ReceivedQuantity);
}
