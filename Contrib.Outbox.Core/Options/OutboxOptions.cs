namespace Contrib.Outbox.Core.Options;

/// <summary>
/// Engine options, common to all providers. Storage-specific options
/// (Mongo collection name, SQL table name, Redis key prefix, ...) live in the
/// options classes owned by each provider package (e.g. MongoOutboxOptions).
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Maximum number of messages processed per polling cycle.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Frequency of the Worker's PeriodicTimer.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Acquisition timeout for the per-message distributed lock.
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Retention duration for already-sent messages before purge (owned by the provider:
    /// Mongo TTL index, SQL purge job, Redis expiration, ...).
    /// </summary>
    public TimeSpan SentMessageRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Threshold beyond which an unsent message triggers a Degraded HealthCheck status.
    /// </summary>
    public TimeSpan SlaBreachThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
