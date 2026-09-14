using Contrib.Outbox.Core.Abstractions;

namespace Contrib.Outbox.Core.Internal;

internal sealed class OutboxEnlister : IOutboxEnlister
{
    private readonly IOutboxStore _store;

    public OutboxEnlister(IOutboxStore store)
    {
        _store = store;
    }

    public Task EnlistAsync<T>(T @event, object? transactionContext = null, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        return _store.EnlistAsync(@event, transactionContext, cancellationToken);
    }
}
