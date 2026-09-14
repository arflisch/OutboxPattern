using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Models;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contrib.Outbox.Core.Internal;

internal sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxStore _store;
    private readonly IOutboxDistributedLock _distributedLock;
    private readonly IMessagePublisher _publisher;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxStore store,
        IOutboxDistributedLock distributedLock,
        IMessagePublisher publisher,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _store = store;
        _distributedLock = distributedLock;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var pendingMessages = await _store.GetPendingBatchAsync(_options.BatchSize, now, cancellationToken);

        var processed = 0;
        var succeeded = 0;
        var exceptions = new List<Exception>();

        foreach (var message in pendingMessages)
        {
            // 2. Acquire the lock.
            var lockKey = $"outbox:{message.Id}";
            await using var lockHandle = await _distributedLock.TryAcquireAsync(
                lockKey, _options.LockTimeout, cancellationToken);

            if (lockHandle is null)
            {
                continue;
            }

            processed++;

            try
            {
                await _publisher.PublishAsync(message, cancellationToken);

                await _store.MarkAsSentAsync(message.Id, DateTime.UtcNow, cancellationToken);
                succeeded++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                exceptions.Add(ex);
            }
        }

        return new OutboxProcessingResult
        {
            ProcessedCount = processed,
            SucceededCount = succeeded,
            FailedCount = processed - succeeded,
            Exceptions = exceptions
        };
    }
}
