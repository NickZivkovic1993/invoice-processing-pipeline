using FluentAssertions;
using LedgerFlow.Core.Domain;
using LedgerFlow.Core.Matching;
using LedgerFlow.Infrastructure.Erp;
using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Persistence.Entities;
using LedgerFlow.Infrastructure.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LedgerFlow.Infrastructure.Tests;

/// <summary>
/// The extract→match→post orchestration against a real (in-memory) DbContext and a fake ERP:
/// clean invoices post and are recorded as Posted; dirty ones land in the exception queue with
/// their typed exceptions; and a posted invoice arriving twice is caught by the history check.
/// </summary>
public class InvoiceProcessorTests
{
    private sealed class FakeErp : IErpPostingClient
    {
        public List<string> Posted { get; } = [];

        public Task<ErpPostingResult> PostAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            Posted.Add(invoice.InvoiceNumber);
            return Task.FromResult(new ErpPostingResult(true, $"ERP-{invoice.InvoiceNumber}"));
        }
    }

    private static LedgerFlowDbContext NewDb() =>
        new(new DbContextOptionsBuilder<LedgerFlowDbContext>()
            .UseInMemoryDatabase($"ledgerflow-{Guid.NewGuid():N}")
            .Options);

    private static InvoiceProcessor Processor(LedgerFlowDbContext db, FakeErp erp) =>
        new(new ThreeWayMatcher(), db, erp, NullLogger<InvoiceProcessor>.Instance);

    private static (Invoice Invoice, PurchaseOrder Po, GoodsReceipt Receipt) CleanTrio()
    {
        var line = new InvoiceLine
        {
            Sku = "SKU-1",
            Description = "Widget",
            Quantity = 10m,
            UnitPrice = new Money(5.00m, "EUR"),
        };
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-1",
            SupplierId = "SUP-001",
            InvoiceDate = new DateOnly(2026, 6, 1),
            PurchaseOrderNumber = "PO-1",
            Total = new Money(50.00m, "EUR"),
            Lines = [line],
        };
        var po = new PurchaseOrder
        {
            PurchaseOrderNumber = "PO-1",
            SupplierId = "SUP-001",
            Currency = "EUR",
            Lines = [new PurchaseOrderLine { Sku = "SKU-1", OrderedQuantity = 10m, UnitPrice = new Money(5.00m, "EUR") }],
        };
        var receipt = new GoodsReceipt
        {
            ReceiptNumber = "GR-1",
            PurchaseOrderNumber = "PO-1",
            ReceivedOn = new DateOnly(2026, 5, 30),
            Lines = [new GoodsReceiptLine { Sku = "SKU-1", ReceivedQuantity = 10m }],
        };
        return (invoice, po, receipt);
    }

    [Fact]
    public async Task CleanInvoice_IsPostedToErp_AndRecordedAsPosted()
    {
        await using var db = NewDb();
        var erp = new FakeErp();
        var (invoice, po, receipt) = CleanTrio();

        var result = await Processor(db, erp).ProcessAsync(invoice, po, [receipt]);

        result.Decision.Should().Be(MatchDecision.AutoPost);
        erp.Posted.Should().ContainSingle().Which.Should().Be("INV-1");
        var record = await db.Invoices.Include(i => i.Exceptions).SingleAsync();
        record.Status.Should().Be(InvoiceStatus.Posted);
        record.Exceptions.Should().BeEmpty();
        record.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DirtyInvoice_LandsInExceptionQueue_AndIsNotPosted()
    {
        await using var db = NewDb();
        var erp = new FakeErp();
        var (invoice, po, receipt) = CleanTrio();
        var overpriced = invoice with
        {
            Lines = [invoice.Lines[0] with { UnitPrice = new Money(9.99m, "EUR") }],
            Total = new Money(99.90m, "EUR"),
        };

        var result = await Processor(db, erp).ProcessAsync(overpriced, po, [receipt]);

        result.Decision.Should().Be(MatchDecision.Review);
        erp.Posted.Should().BeEmpty();
        var record = await db.Invoices.Include(i => i.Exceptions).SingleAsync();
        record.Status.Should().Be(InvoiceStatus.NeedsReview);
        record.Exceptions.Should().Contain(e => e.Code == ExceptionCode.PriceOverTolerance);
    }

    [Fact]
    public async Task PostedInvoiceArrivingAgain_IsFlaggedAsDuplicate()
    {
        await using var db = NewDb();
        var erp = new FakeErp();
        var processor = Processor(db, erp);
        var (invoice, po, receipt) = CleanTrio();

        await processor.ProcessAsync(invoice, po, [receipt]);          // first pass: posts
        var second = await processor.ProcessAsync(invoice, po, [receipt]); // resent email

        second.Decision.Should().Be(MatchDecision.Review);
        second.Exceptions.Should().Contain(e => e.Code == ExceptionCode.DuplicateInvoice);
        erp.Posted.Should().HaveCount(1, "the duplicate must not post a second time");
    }
}
