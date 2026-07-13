using LedgerFlow.Infrastructure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Functions;

/// <summary>
/// Capture stage. When an invoice document lands in the <c>invoices-inbox</c> blob container
/// (dropped there by the mailbox connector or the upload UI), publish a pointer to the extraction
/// queue. The document bytes stay in blob storage; only a reference travels on the bus.
/// </summary>
public sealed class CaptureFunction
{
    private readonly IPipelineQueue _queue;
    private readonly ILogger<CaptureFunction> _logger;

    public CaptureFunction(IPipelineQueue queue, ILogger<CaptureFunction> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    [Function(nameof(CaptureFunction))]
    public async Task RunAsync(
        [BlobTrigger("invoices-inbox/{name}", Connection = "AzureWebJobsStorage")] byte[] _,
        string name,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var correlationId = context.InvocationId;
        var blobUri = $"invoices-inbox/{name}";
        var contentType = GuessContentType(name);

        _logger.LogInformation("Captured {Blob}; enqueuing for extraction ({Correlation}).", blobUri, correlationId);

        await _queue.EnqueueAsync(new DocumentReceivedMessage(blobUri, contentType, correlationId), cancellationToken);
    }

    private static string GuessContentType(string name) =>
        Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" or ".tif" => "image/tiff",
            _ => "application/octet-stream",
        };
}
