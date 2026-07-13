using System.Text.Json;
using Azure.Storage.Blobs;
using LedgerFlow.Core.Domain;
using LedgerFlow.Infrastructure.Erp;
using LedgerFlow.Infrastructure.Extraction;
using LedgerFlow.Infrastructure.Messaging;
using LedgerFlow.Infrastructure.Pipeline;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Functions;

/// <summary>
/// Extraction + matching stage. Triggered by a <see cref="DocumentReceivedMessage"/> on the ingest
/// queue: download the document, extract it with Document Intelligence, pull the PO and receipts,
/// then hand the trio to <see cref="InvoiceProcessor"/>, which posts clean invoices and queues the
/// rest. Throwing lets Service Bus retry and, after max deliveries, dead-letter — which is the
/// behaviour we want for a transient extraction or ERP outage.
/// </summary>
public sealed class ProcessInvoiceFunction
{
    private readonly BlobServiceClient _blobs;
    private readonly IDocumentExtractor _extractor;
    private readonly IReferenceDataProvider _referenceData;
    private readonly InvoiceProcessor _processor;
    private readonly ILogger<ProcessInvoiceFunction> _logger;

    public ProcessInvoiceFunction(
        BlobServiceClient blobs,
        IDocumentExtractor extractor,
        IReferenceDataProvider referenceData,
        InvoiceProcessor processor,
        ILogger<ProcessInvoiceFunction> logger)
    {
        _blobs = blobs;
        _extractor = extractor;
        _referenceData = referenceData;
        _processor = processor;
        _logger = logger;
    }

    [Function(nameof(ProcessInvoiceFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger("%IngestQueueName%", Connection = "ServiceBusConnection")] string body,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<DocumentReceivedMessage>(body)
            ?? throw new InvalidOperationException("Ingest message was empty or malformed.");

        var (container, blobName) = SplitBlobUri(message.BlobUri);
        var blobClient = _blobs.GetBlobContainerClient(container).GetBlobClient(blobName);

        await using var stream = (await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)).Value.Content;
        var invoice = await _extractor.ExtractAsync(stream, message.ContentType, cancellationToken);

        PurchaseOrderLookup lookup = invoice.PurchaseOrderNumber is { } poNumber
            ? new(await _referenceData.GetPurchaseOrderAsync(poNumber, cancellationToken),
                  await _referenceData.GetReceiptsAsync(poNumber, cancellationToken))
            : new(null, Array.Empty<GoodsReceipt>());

        var result = await _processor.ProcessAsync(invoice, lookup.PurchaseOrder, lookup.Receipts, cancellationToken);

        _logger.LogInformation(
            "Processed {Invoice}: {Decision} ({Correlation}).",
            invoice.InvoiceNumber, result.Decision, message.CorrelationId);
    }

    private static (string Container, string Blob) SplitBlobUri(string blobUri)
    {
        var parts = blobUri.Split('/', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : ("invoices-inbox", blobUri);
    }

    private readonly record struct PurchaseOrderLookup(
        PurchaseOrder? PurchaseOrder,
        IReadOnlyCollection<GoodsReceipt> Receipts);
}
