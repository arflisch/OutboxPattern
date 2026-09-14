namespace Contrib.Outbox.Mongo;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string MessageType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreationTime { get; init; }
    public DateTime? SentOn { get; set; }
    public DateTime? NotBefore { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed class OutboxLock
{
    public string ResourceKey { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
