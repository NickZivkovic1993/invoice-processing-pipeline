using FluentAssertions;
using LedgerFlow.Core.Domain;
using LedgerFlow.Core.Matching;
using Xunit;

namespace LedgerFlow.Core.Tests;

public class ThreeWayMatcherTests
{
    private readonly ThreeWayMatcher _matcher = new();

    [Fact]
    public void CleanInvoice_AutoPosts()
    {
        var result = _matcher.Match(Builders.Invoice(), Builders.Po_(), new[] { Builders.Receipt() });

        result.Decision.Should().Be(MatchDecision.AutoPost);
        result.IsClean.Should().BeTrue();
    }

    [Fact]
    public void PriceWithinTolerance_AutoPosts()
    {
        // PO price 5.00, 2% + 0.01 tolerance => 0.11 headroom; 5.10 is inside it.
        var invoice = Builders.Invoice(new[] { Builders.Line(unitPrice: 5.10m) });
        var po = Builders.Po_(new[] { Builders.PoLine(unitPrice: 5.00m) });

        var result = _matcher.Match(invoice, po, new[] { Builders.Receipt() });

        result.Decision.Should().Be(MatchDecision.AutoPost);
    }

    [Fact]
    public void PriceJustOverTolerance_RaisesPriceException()
    {
        var invoice = Builders.Invoice(new[] { Builders.Line(unitPrice: 5.20m) });
        var po = Builders.Po_(new[] { Builders.PoLine(unitPrice: 5.00m) });

        var result = _matcher.Match(invoice, po, new[] { Builders.Receipt() });

        result.Decision.Should().Be(MatchDecision.Review);
        result.Exceptions.Should().ContainSingle()
            .Which.Code.Should().Be(ExceptionCode.PriceOverTolerance);
    }

    [Fact]
    public void QuantityOverReceipt_RaisesException_WhenNoOverTolerance()
    {
        var invoice = Builders.Invoice(new[] { Builders.Line(qty: 12m) });
        var receipt = Builders.Receipt(new[] { Builders.ReceiptLine(received: 10m) });

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { receipt });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.QuantityOverReceipt);
    }

    [Fact]
    public void PartialReceipts_SumAcrossDeliveries()
    {
        // Invoiced 10; two receipts of 6 + 4 fully cover it.
        var invoice = Builders.Invoice(new[] { Builders.Line(qty: 10m) });
        var receipts = new[]
        {
            Builders.Receipt(new[] { Builders.ReceiptLine(received: 6m) }),
            Builders.Receipt(new[] { Builders.ReceiptLine(received: 4m) }),
        };

        var result = _matcher.Match(invoice, Builders.Po_(), receipts);

        result.Decision.Should().Be(MatchDecision.AutoPost);
    }

    [Fact]
    public void NoReceiptForLine_RaisesNoGoodsReceipt()
    {
        var result = _matcher.Match(Builders.Invoice(), Builders.Po_(), Array.Empty<GoodsReceipt>());

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.NoGoodsReceipt);
    }

    [Fact]
    public void DuplicateInvoice_RaisesDuplicate()
    {
        var invoice = Builders.Invoice();
        var history = new StubHistory(invoice.DeduplicationKey);

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() }, history);

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.DuplicateInvoice);
    }

    [Fact]
    public void MissingPurchaseOrder_ShortCircuits()
    {
        var result = _matcher.Match(Builders.Invoice(), purchaseOrder: null, new[] { Builders.Receipt() });

        result.Decision.Should().Be(MatchDecision.Review);
        result.Exceptions.Should().ContainSingle()
            .Which.Code.Should().Be(ExceptionCode.MissingPurchaseOrder);
    }

    [Fact]
    public void CurrencyMismatch_ShortCircuitsBeforeLineChecks()
    {
        var invoice = Builders.Invoice() with { Total = new Money(50m, "USD") };
        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() });

        result.Exceptions.Should().ContainSingle()
            .Which.Code.Should().Be(ExceptionCode.CurrencyMismatch);
    }

    [Fact]
    public void SupplierMismatch_RaisesException()
    {
        var po = Builders.Po_(supplier: "SUP-999");
        var result = _matcher.Match(Builders.Invoice(), po, new[] { Builders.Receipt() });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.SupplierMismatch);
    }

    [Fact]
    public void UnorderedItem_RaisesException()
    {
        var invoice = Builders.Invoice(new[] { Builders.Line(sku: "SKU-ROGUE") });
        var receipt = Builders.Receipt(new[] { Builders.ReceiptLine(sku: "SKU-ROGUE") });

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { receipt });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.UnorderedItem);
    }

    [Fact]
    public void LowConfidence_RaisesException()
    {
        var invoice = Builders.Invoice(confidence: 0.55);
        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.LowConfidence);
    }

    [Fact]
    public void TotalsMismatch_RaisesException()
    {
        // Two lines of 10 x 5.00 = 100.00, but the stated total is wrong.
        var invoice = Builders.Invoice(
            new[] { Builders.Line() },
            total: new Money(999.00m, Builders.Currency));

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.TotalsMismatch);
    }

    [Fact]
    public void TotalsWithTax_ReconcileCleanly()
    {
        var line = Builders.Line(qty: 10m, unitPrice: 5.00m); // 50.00
        var tax = new Money(10.00m, Builders.Currency);
        var invoice = Builders.Invoice(new[] { line }, tax: tax); // total computed as 60.00

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() });

        result.Decision.Should().Be(MatchDecision.AutoPost);
    }

    [Fact]
    public void MultipleProblems_AllReported()
    {
        var invoice = Builders.Invoice(
            new[] { Builders.Line(unitPrice: 9.00m, qty: 30m) },
            confidence: 0.40);

        var result = _matcher.Match(invoice, Builders.Po_(), new[] { Builders.Receipt() });

        result.Exceptions.Select(e => e.Code).Should().Contain(new[]
        {
            ExceptionCode.LowConfidence,
            ExceptionCode.PriceOverTolerance,
            ExceptionCode.QuantityOverReceipt,
        });
    }

    private sealed class StubHistory : IInvoiceHistory
    {
        private readonly string _seen;
        public StubHistory(string seen) => _seen = seen;
        public bool HasSeen(string deduplicationKey) => deduplicationKey == _seen;
    }
}
