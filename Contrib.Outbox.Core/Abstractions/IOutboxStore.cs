using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Contrat de persistance que doit implémenter chaque provider de stockage
/// (MongoDB, PostgreSQL, MySQL, Redis, ou toute autre techno). Le moteur de la librairie
/// (Enlister, Processor, Worker, HealthCheck) ne dépend que de cette interface : brancher
/// une nouvelle base de données revient à écrire une seule implémentation de IOutboxStore.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Sérialise et persiste le message. <paramref name="transactionContext"/> est un objet
    /// opaque représentant la transaction/session native de la base de données hôte
    /// (ex: IClientSessionHandle pour Mongo, DbTransaction pour du SQL, ITransaction pour Redis).
    /// Chaque implémentation de IOutboxStore définit le type qu'elle attend et lève une
    /// InvalidCastException explicite si un autre type lui est passé. Peut être null si le
    /// provider ne supporte/nécessite pas de transaction ambiante (ex: Redis en mode non transactionnel).
    /// </summary>
    Task EnlistAsync<T>(T @event, object? transactionContext, CancellationToken cancellationToken = default)
        where T : IEvent;

    /// <summary>
    /// Retourne jusqu'à <paramref name="batchSize"/> messages éligibles au traitement
    /// (SentOn null, NotBefore atteint, non expirés), triés du plus ancien au plus récent.
    /// </summary>
    Task<IReadOnlyList<StandardOutboxMessage>> GetPendingBatchAsync(
        int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marque le message comme envoyé.
    /// </summary>
    Task MarkAsSentAsync(Guid messageId, DateTime sentOnUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compte les messages non envoyés, non expirés, créés avant <paramref name="staleBeforeUtc"/>
    /// (utilisé par le HealthCheck pour détecter un SLA breach).
    /// </summary>
    Task<long> CountStaleAsync(
        DateTime staleBeforeUtc, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée/vérifie l'infrastructure nécessaire (index MongoDB, table + index SQL,
    /// structures Redis...). Idempotent, à appeler une fois au démarrage.
    /// </summary>
    Task EnsureInfrastructureAsync(CancellationToken cancellationToken = default);
}
