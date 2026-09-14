using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Abstraction of the actual message publishing (RabbitMQ, Kafka, Service Bus, ...).
/// Intentionally without retry here: retry responsibility, if desired, stays on the host side.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(StandardOutboxMessage message, CancellationToken cancellationToken = default);
}
