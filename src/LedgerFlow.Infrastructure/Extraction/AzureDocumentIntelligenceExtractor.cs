using Azure;
using Azure.AI.DocumentIntelligence;
using LedgerFlow.Core.Domain;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Infrastructure.Extraction;

/// <summary>
/// Extracts invoice fields with the Azure AI Document Intelligence prebuilt-invoice model and maps
/// its <c>AnalyzedDocument</c> onto our <see cref="Invoice"/>. The lowest per-field confidence is
/// propagated onto <see cref="Invoice.ExtractionConfidence"/> so the matcher can route uncertain
/// documents to a human.
/// </summary>
public sealed class AzureDocumentIntelligenceExtractor : IDocumentExtractor
{
    private const string InvoiceModelId = "prebuilt-invoice";

    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<AzureDocumentIntelligenceExtractor> _logger;

    public AzureDocumentIntelligenceExtractor(
        DocumentIntelligenceClient client,
        ILogger<AzureDocumentIntelligenceExtractor> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Invoice> ExtractAsync(Stream document, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            InvoiceModelId,
            BinaryData.FromBytes(buffer.ToArray()),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var analyzed = operation.Value.Documents.FirstOrDefault()
            ?? throw new InvalidOperationException("Document Intelligence returned no invoice document.");

        var confidences = new List<double>();
        var invoice = InvoiceFieldMapper.Map(analyzed, confidences);

        _logger.LogInformation(
            "Extracted invoice {InvoiceNumber} with {LineCount} lines at confidence {Confidence:P0}.",
            invoice.InvoiceNumber, invoice.Lines.Count, invoice.ExtractionConfidence);

        return invoice;
    }
}
