using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace LedgerFlow.Infrastructure.Messaging;

/// <summary>Publishes <see cref="DocumentReceivedMessage"/>s onto a Service Bus queue.</summary>
public sealed class ServiceBusPipelineQueue : IPipelineQueue, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ServiceBusPipelineQueue(ServiceBusClient client, string queueName) =>
        _sender = client.CreateSender(queueName);

    public async Task EnqueueAsync(DocumentReceivedMessage message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            MessageId = message.CorrelationId,
        };

        await _sender.SendMessageAsync(sbMessage, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
