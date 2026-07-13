namespace LedgerFlow.Infrastructure.Messaging;

/// <summary>
/// Hands a captured document off to the extraction stage. Backed by Azure Service Bus in production
/// (<see cref="ServiceBusPipelineQueue"/>); a port so the capture stage doesn't bind to the broker.
/// </summary>
public interface IPipelineQueue
{
    Task EnqueueAsync(DocumentReceivedMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Pointer to a document that has landed in blob storage and is ready to extract.</summary>
public sealed record DocumentReceivedMessage(string BlobUri, string ContentType, string CorrelationId);
