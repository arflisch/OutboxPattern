using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Abstraction de la publication effective du message (RabbitMQ, Kafka, Service Bus, ...).
/// Volontairement sans retry ici : la responsabilité du retry, si voulue, reste côté hôte.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(StandardOutboxMessage message, CancellationToken cancellationToken = default);
}
