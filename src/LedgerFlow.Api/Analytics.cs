using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Api;

/// <summary>The numbers a finance lead actually asks for: throughput, STP rate, why invoices stall.</summary>
public sealed record AnalyticsSummary(
    int TotalProcessed,
    int AutoPosted,
    int AwaitingReview,
    int Rejected,
    double StraightThroughRate,
    IReadOnlyList<ExceptionReasonCount> TopExceptionReasons,
    IReadOnlyList<SupplierVolume> TopSuppliers);

public sealed record ExceptionReasonCount(string Code, int Count);

public sealed record SupplierVolume(string SupplierId, int Invoices, decimal TotalAmount);

public static class Analytics
{
    public static async Task<AnalyticsSummary> ComputeAsync(LedgerFlowDbContext db, DateTimeOffset since)
    {
        var window = db.Invoices.Where(i => i.ReceivedAt >= since);

        var total = await window.CountAsync();
        var posted = await window.CountAsync(i => i.Status == InvoiceStatus.Posted);
        var review = await window.CountAsync(i => i.Status == InvoiceStatus.NeedsReview);
        var rejected = await window.CountAsync(i => i.Status == InvoiceStatus.Rejected);

        // Auto-posted = posted without a human touching it (no exceptions recorded).
        var autoPosted = await window.CountAsync(i =>
            i.Status == InvoiceStatus.Posted && !i.Exceptions.Any());

        var reasons = await db.Exceptions
            .Where(e => window.Select(i => i.Id).Contains(e.InvoiceId))
            .GroupBy(e => e.Code)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var suppliers = await window
            .GroupBy(i => i.SupplierId)
            .Select(g => new SupplierVolume(g.Key, g.Count(), g.Sum(i => i.TotalAmount)))
            .OrderByDescending(s => s.Invoices)
            .Take(5)
            .ToListAsync();

        return new AnalyticsSummary(
            TotalProcessed: total,
            AutoPosted: autoPosted,
            AwaitingReview: review,
            Rejected: rejected,
            StraightThroughRate: total == 0 ? 0 : (double)autoPosted / total,
            TopExceptionReasons: reasons.Select(r => new ExceptionReasonCount(r.Key.ToString(), r.Count)).ToList(),
            TopSuppliers: suppliers);
    }
}
