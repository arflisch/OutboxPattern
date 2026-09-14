using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

public interface IOutboxProcessor
{
    Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
}
