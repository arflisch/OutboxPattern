namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Enlister (write side): application entry point for adding an outbox message.
/// Delegates to IOutboxStore.EnlistAsync — see that contract for the semantics of transactionContext.
/// </summary>
public interface IOutboxEnlister
{
    Task EnlistAsync<T>(T @event, object? transactionContext = null, CancellationToken cancellationToken = default)
        where T : IEvent;
}
