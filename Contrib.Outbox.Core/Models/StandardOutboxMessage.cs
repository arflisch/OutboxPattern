namespace Contrib.Outbox.Core.Models;

/// <summary>
/// Representation of an outbox message, independent of any storage engine.
/// Each provider (Mongo, Sql, Redis...) is responsible for mapping this model
/// to its own persistence format (document, row, hash...).
/// </summary>
public sealed class StandardOutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Qualified name of the message type, used for (de)serialization of the Payload.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Serialized payload (JSON) of the business message.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Actual send date. Null while the message is pending.
    /// </summary>
    public DateTime? SentOn { get; set; }

    /// <summary>
    /// Deferred send: the message is only eligible for polling from this date onward.
    /// </summary>
    public DateTime? NotBefore { get; init; }

    /// <summary>
    /// Application-level time-to-live: past this date, the message is no longer published.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
