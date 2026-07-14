using FluentAssertions;
using LedgerFlow.Core.Matching;
using Xunit;

namespace LedgerFlow.Core.Tests;

public class SupplierPoliciesTests
{
    [Fact]
    public void UnknownSupplier_GetsDefaultPolicy()
    {
        var policies = new SupplierPolicies();

        policies.For("SUP-anything").Should().Be(MatchTolerances.Default);
    }

    [Fact]
    public void OverriddenSupplier_GetsItsOwnPolicy()
    {
        var strict = new MatchTolerances { UnitPriceRelativeTolerance = 0m, UnitPriceAbsoluteTolerance = 0m };
        var policies = new SupplierPolicies(overrides: new Dictionary<string, MatchTolerances>
        {
            ["SUP-001"] = strict,
        });

        policies.For("SUP-001").Should().Be(strict);
        policies.For("sup-001").Should().Be(strict, "supplier IDs are matched case-insensitively");
    }

    [Fact]
    public void StrictPolicy_TurnsToleratedDriftIntoAnException()
    {
        // 5.10 vs 5.00 passes the default 2% band but fails a zero-tolerance supplier.
        var invoice = Builders.Invoice(new[] { Builders.Line(unitPrice: 5.10m) });
        var po = Builders.Po_(new[] { Builders.PoLine(unitPrice: 5.00m) });
        var strict = new SupplierPolicies(overrides: new Dictionary<string, MatchTolerances>
        {
            [Builders.Supplier] = new() { UnitPriceRelativeTolerance = 0m, UnitPriceAbsoluteTolerance = 0m },
        });

        var result = strict.MatcherFor(invoice.SupplierId).Match(invoice, po, new[] { Builders.Receipt() });

        result.Exceptions.Should().Contain(e => e.Code == ExceptionCode.PriceOverTolerance);
    }
}
