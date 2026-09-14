using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Persistence contract that each storage provider must implement
/// (MongoDB, PostgreSQL, MySQL, Redis, or any other technology). The library engine
/// (Enlister, Processor, Worker, HealthCheck) depends only on this interface: plugging in
/// a new database comes down to writing a single IOutboxStore implementation.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Serializes and persists the message. <paramref name="transactionContext"/> is an opaque
    /// object representing the host database's native transaction/session
    /// (e.g. IClientSessionHandle for Mongo, DbTransaction for SQL, ITransaction for Redis).
    /// Each IOutboxStore implementation defines the type it expects and throws an explicit
    /// InvalidCastException if another type is passed to it. Can be null if the
    /// provider does not support/require an ambient transaction (e.g. Redis in non-transactional mode).
    /// </summary>
    Task EnlistAsync<T>(T @event, object? transactionContext, CancellationToken cancellationToken = default)
        where T : IEvent;

    /// <summary>
    /// Returns up to <paramref name="batchSize"/> messages eligible for processing
    /// (SentOn null, NotBefore reached, not expired), sorted from oldest to newest.
    /// </summary>
    Task<IReadOnlyList<StandardOutboxMessage>> GetPendingBatchAsync(
        int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the message as sent.
    /// </summary>
    Task MarkAsSentAsync(Guid messageId, DateTime sentOnUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts unsent, non-expired messages created before <paramref name="staleBeforeUtc"/>
    /// (used by the HealthCheck to detect an SLA breach).
    /// </summary>
    Task<long> CountStaleAsync(
        DateTime staleBeforeUtc, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates/verifies the required infrastructure (MongoDB indexes, SQL table + indexes,
    /// Redis structures...). Idempotent, to be called once at startup.
    /// </summary>
    Task EnsureInfrastructureAsync(CancellationToken cancellationToken = default);
}
