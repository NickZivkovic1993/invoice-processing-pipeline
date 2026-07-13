using LedgerFlow.Core.Matching;
using LedgerFlow.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Infrastructure.Persistence;

/// <summary>
/// The exception queue's store: every invoice that could not auto-post lands here as an
/// <see cref="InvoiceRecord"/> with its typed <see cref="ExceptionRecord"/>s, so a reviewer can
/// work the backlog and the <see cref="IInvoiceHistory"/> port can answer "have we seen this?".
/// </summary>
public sealed class LedgerFlowDbContext : DbContext, IInvoiceHistory
{
    public LedgerFlowDbContext(DbContextOptions<LedgerFlowDbContext> options) : base(options)
    {
    }

    public DbSet<InvoiceRecord> Invoices => Set<InvoiceRecord>();
    public DbSet<ExceptionRecord> Exceptions => Set<ExceptionRecord>();

    public bool HasSeen(string deduplicationKey) =>
        Invoices.Any(i => i.DeduplicationKey == deduplicationKey && i.Status == InvoiceStatus.Posted);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvoiceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.SupplierId).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeduplicationKey).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.DeduplicationKey);
            e.HasIndex(x => x.Status);
            e.HasMany(x => x.Exceptions).WithOne().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExceptionRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasConversion<string>().HasMaxLength(40).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(64);
        });
    }
}
