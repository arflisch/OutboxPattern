namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Abstraction of the distributed lock system. Provided either by the host application
/// (reusing an existing system), or by a provider implementation
/// (e.g. Contrib.Outbox.Redis.RedisDistributedLock, Contrib.Outbox.Sql.SqlAdvisoryLock).
/// </summary>
public interface IOutboxDistributedLock
{
    Task<IOutboxLockHandle?> TryAcquireAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public interface IOutboxLockHandle : IAsyncDisposable
{
    string ResourceKey { get; }
}
