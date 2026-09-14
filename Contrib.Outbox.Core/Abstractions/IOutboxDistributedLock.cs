namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Abstraction du système de lock distribué. Fourni soit par l'application hôte
/// (réutilisation d'un système existant), soit par une implémentation d'un provider
/// (ex: Contrib.Outbox.Redis.RedisDistributedLock, Contrib.Outbox.Sql.SqlAdvisoryLock).
/// </summary>
public interface IOutboxDistributedLock
{
    Task<IOutboxLockHandle?> TryAcquireAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public interface IOutboxLockHandle : IAsyncDisposable
{
    string ResourceKey { get; }
}
