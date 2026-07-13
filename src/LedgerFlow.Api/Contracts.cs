using LedgerFlow.Infrastructure.Persistence.Entities;

namespace LedgerFlow.Api;

/// <summary>Wire shapes for the exception-queue UI. Kept separate from EF entities so the two evolve independently.</summary>
public sealed record ExceptionQueueItem(
    Guid Id,
    string InvoiceNumber,
    string SupplierId,
    string Currency,
    decimal TotalAmount,
    double ExtractionConfidence,
    string Status,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<ExceptionDetail> Exceptions)
{
    public static ExceptionQueueItem From(InvoiceRecord record) => new(
        record.Id,
        record.InvoiceNumber,
        record.SupplierId,
        record.Currency,
        record.TotalAmount,
        record.ExtractionConfidence,
        record.Status.ToString(),
        record.ReceivedAt,
        record.Exceptions.Select(e => new ExceptionDetail(e.Code.ToString(), e.Message, e.Sku)).ToList());
}

public sealed record ExceptionDetail(string Code, string Message, string? Sku);

public sealed record ResolveRequest(string? Note);
