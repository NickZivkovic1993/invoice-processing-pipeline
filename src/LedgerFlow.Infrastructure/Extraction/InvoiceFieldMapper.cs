using System.Globalization;
using Azure.AI.DocumentIntelligence;
using LedgerFlow.Core.Domain;

namespace LedgerFlow.Infrastructure.Extraction;

/// <summary>
/// Maps a Document Intelligence <see cref="AnalyzedDocument"/> (prebuilt-invoice schema) onto our
/// domain <see cref="Invoice"/>. Isolated from the client so the field-name knowledge lives in one
/// place and is unit-testable if a fixture document is ever captured.
/// </summary>
internal static class InvoiceFieldMapper
{
    public static Invoice Map(AnalyzedDocument analyzed, List<double> confidenceSink)
    {
        var fields = analyzed.Fields;
        var currency = ReadCurrency(fields);

        var lines = ReadLines(fields, currency, confidenceSink);
        var total = ReadMoney(fields, "InvoiceTotal", currency, confidenceSink)
            ?? lines.Aggregate(Money.Zero(currency), (a, l) => a + l.LineTotal);

        return new Invoice
        {
            InvoiceNumber = ReadString(fields, "InvoiceId", confidenceSink) ?? "UNKNOWN",
            SupplierId = ReadString(fields, "VendorName", confidenceSink) ?? "UNKNOWN",
            InvoiceDate = ReadDate(fields, "InvoiceDate", confidenceSink) ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseOrderNumber = ReadString(fields, "PurchaseOrder", confidenceSink),
            Total = total,
            Tax = ReadMoney(fields, "TotalTax", currency, confidenceSink),
            Lines = lines,
            ExtractionConfidence = confidenceSink.Count > 0 ? confidenceSink.Min() : 1.0,
        };
    }

    private static IReadOnlyList<InvoiceLine> ReadLines(
        IReadOnlyDictionary<string, DocumentField> fields, string currency, List<double> confidenceSink)
    {
        if (!fields.TryGetValue("Items", out var items) || items.FieldType != DocumentFieldType.List)
        {
            return Array.Empty<InvoiceLine>();
        }

        var result = new List<InvoiceLine>();
        foreach (var item in items.ValueList)
        {
            if (item.FieldType != DocumentFieldType.Dictionary)
            {
                continue;
            }

            var obj = item.ValueDictionary;
            result.Add(new InvoiceLine
            {
                Sku = ReadString(obj, "ProductCode", confidenceSink) ?? "UNKNOWN",
                Description = ReadString(obj, "Description", confidenceSink) ?? string.Empty,
                Quantity = ReadDecimal(obj, "Quantity", confidenceSink) ?? 0m,
                UnitPrice = ReadMoney(obj, "UnitPrice", currency, confidenceSink) ?? Money.Zero(currency),
            });
        }

        return result;
    }

    private static string ReadCurrency(IReadOnlyDictionary<string, DocumentField> fields) =>
        fields.TryGetValue("InvoiceTotal", out var total)
        && total.FieldType == DocumentFieldType.Currency
        && !string.IsNullOrWhiteSpace(total.ValueCurrency.CurrencyCode)
            ? total.ValueCurrency.CurrencyCode
            : "USD";

    private static string? ReadString(
        IReadOnlyDictionary<string, DocumentField> fields, string key, List<double> confidenceSink)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return null;
        }

        Track(field, confidenceSink);
        return field.FieldType == DocumentFieldType.String ? field.ValueString : field.Content;
    }

    private static Money? ReadMoney(
        IReadOnlyDictionary<string, DocumentField> fields, string key, string currency, List<double> confidenceSink)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return null;
        }

        Track(field, confidenceSink);
        if (field.FieldType == DocumentFieldType.Currency)
        {
            var code = field.ValueCurrency.CurrencyCode;
            return new Money((decimal)field.ValueCurrency.Amount, string.IsNullOrWhiteSpace(code) ? currency : code);
        }

        var parsed = ReadDecimal(fields, key, confidenceSink);
        return parsed is null ? null : new Money(parsed.Value, currency);
    }

    private static decimal? ReadDecimal(
        IReadOnlyDictionary<string, DocumentField> fields, string key, List<double> confidenceSink)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return null;
        }

        Track(field, confidenceSink);
        if (field.FieldType == DocumentFieldType.Double)
        {
            return (decimal)field.ValueDouble;
        }

        if (field.FieldType == DocumentFieldType.Int64 && field.ValueInt64 is { } i)
        {
            return i;
        }

        return decimal.TryParse(field.Content, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static DateOnly? ReadDate(
        IReadOnlyDictionary<string, DocumentField> fields, string key, List<double> confidenceSink)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return null;
        }

        Track(field, confidenceSink);
        return field.FieldType == DocumentFieldType.Date && field.ValueDate is { } d
            ? DateOnly.FromDateTime(d.DateTime)
            : null;
    }

    private static void Track(DocumentField field, List<double> confidenceSink)
    {
        if (field.Confidence is { } c)
        {
            confidenceSink.Add(c);
        }
    }
}
