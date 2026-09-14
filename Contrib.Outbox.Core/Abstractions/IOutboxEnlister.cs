namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Enregistreur (côté écriture) : point d'entrée applicatif pour ajouter un message outbox.
/// Délègue à IOutboxStore.EnlistAsync — voir ce contrat pour la sémantique de transactionContext.
/// </summary>
public interface IOutboxEnlister
{
    Task EnlistAsync<T>(T @event, object? transactionContext = null, CancellationToken cancellationToken = default)
        where T : IEvent;
}
