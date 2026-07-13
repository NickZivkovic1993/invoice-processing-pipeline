using LedgerFlow.Core.Matching;

namespace LedgerFlow.Infrastructure.Persistence.Entities;

public enum InvoiceStatus
{
    /// <summary>Held in the exception queue awaiting a human decision.</summary>
    NeedsReview,

    /// <summary>Auto-posted or approved and sent to the ERP.</summary>
    Posted,

    /// <summary>Rejected by a reviewer; not sent to the ERP.</summary>
    Rejected,
}

/// <summary>Persisted projection of an <see cref="Core.Domain.Invoice"/> plus its match outcome.</summary>
public sealed class InvoiceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string DeduplicationKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal TotalAmount { get; set; }
    public double ExtractionConfidence { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.NeedsReview;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public List<ExceptionRecord> Exceptions { get; set; } = new();
}

/// <summary>Persisted form of a <see cref="MatchException"/>.</summary>
public sealed class ExceptionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public ExceptionCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Sku { get; set; }
}
