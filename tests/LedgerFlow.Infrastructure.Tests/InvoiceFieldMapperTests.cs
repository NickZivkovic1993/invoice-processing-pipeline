using Azure.AI.DocumentIntelligence;
using FluentAssertions;
using LedgerFlow.Infrastructure.Extraction;
using Xunit;

namespace LedgerFlow.Infrastructure.Tests;

/// <summary>
/// Contract tests for the prebuilt-invoice → domain mapping, using the SDK's model factory to
/// build the same shapes Document Intelligence returns. This is the knowledge that would otherwise
/// only be verified against the live service.
/// </summary>
public class InvoiceFieldMapperTests
{
    private static DocumentField Currency(double amount, string code, float confidence = 0.95f) =>
        DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Currency,
            valueCurrency: DocumentIntelligenceModelFactory.CurrencyValue(amount: amount, currencyCode: code),
            confidence: confidence);

    private static DocumentField Text(string value, float confidence = 0.99f) =>
        DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.String,
            valueString: value,
            confidence: confidence);

    private static DocumentField Number(double value, float confidence = 0.9f) =>
        DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Double,
            valueDouble: value,
            confidence: confidence);

    private static DocumentFieldDictionary Fields(Dictionary<string, DocumentField> source) =>
        DocumentIntelligenceModelFactory.DocumentFieldDictionary(source);

    private static AnalyzedDocument Doc(Dictionary<string, DocumentField> fields) =>
        DocumentIntelligenceModelFactory.AnalyzedDocument(documentType: "invoice", fields: Fields(fields));

    [Fact]
    public void Maps_header_fields_and_currency()
    {
        var analyzed = Doc(new Dictionary<string, DocumentField>
        {
            ["InvoiceId"] = Text("INV-42"),
            ["VendorName"] = Text("Meridian Print Ltd"),
            ["PurchaseOrder"] = Text("PO-40918"),
            ["InvoiceTotal"] = Currency(2140.55, "EUR"),
        });

        var confidences = new List<double>();
        var invoice = InvoiceFieldMapper.Map(analyzed, confidences);

        invoice.InvoiceNumber.Should().Be("INV-42");
        invoice.SupplierId.Should().Be("Meridian Print Ltd");
        invoice.PurchaseOrderNumber.Should().Be("PO-40918");
        invoice.Total.Amount.Should().Be(2140.55m);
        invoice.Total.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Lowest_field_confidence_becomes_the_invoice_confidence()
    {
        var analyzed = Doc(new Dictionary<string, DocumentField>
        {
            ["InvoiceId"] = Text("INV-1", confidence: 0.99f),
            ["InvoiceTotal"] = Currency(100, "EUR", confidence: 0.61f),
        });

        var invoice = InvoiceFieldMapper.Map(analyzed, new List<double>());

        invoice.ExtractionConfidence.Should().BeApproximately(0.61, 0.001);
    }

    [Fact]
    public void Line_items_map_from_the_items_list()
    {
        var lineFields = new Dictionary<string, DocumentField>
        {
            ["ProductCode"] = Text("SKU-1"),
            ["Description"] = Text("Digital print run"),
            ["Quantity"] = Number(10),
            ["UnitPrice"] = Currency(5.00, "EUR"),
        };
        var item = DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Dictionary,
            valueDictionary: Fields(lineFields));
        var items = DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.List,
            valueList: new[] { item });

        var analyzed = Doc(new Dictionary<string, DocumentField>
        {
            ["InvoiceId"] = Text("INV-7"),
            ["InvoiceTotal"] = Currency(50.00, "EUR"),
            ["Items"] = items,
        });

        var invoice = InvoiceFieldMapper.Map(analyzed, new List<double>());

        var line = invoice.Lines.Should().ContainSingle().Subject;
        line.Sku.Should().Be("SKU-1");
        line.Quantity.Should().Be(10m);
        line.UnitPrice.Amount.Should().Be(5.00m);
        line.LineTotal.Amount.Should().Be(50.00m);
    }

    [Fact]
    public void Missing_total_falls_back_to_summed_lines()
    {
        var lineFields = new Dictionary<string, DocumentField>
        {
            ["ProductCode"] = Text("SKU-1"),
            ["Description"] = Text("Widget"),
            ["Quantity"] = Number(2),
            ["UnitPrice"] = Currency(7.50, "USD"),
        };
        var item = DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Dictionary, valueDictionary: Fields(lineFields));
        var items = DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.List, valueList: new[] { item });

        var analyzed = Doc(new Dictionary<string, DocumentField>
        {
            ["InvoiceId"] = Text("INV-8"),
            ["Items"] = items,
        });

        var invoice = InvoiceFieldMapper.Map(analyzed, new List<double>());

        invoice.Total.Amount.Should().Be(15.00m);
    }
}
