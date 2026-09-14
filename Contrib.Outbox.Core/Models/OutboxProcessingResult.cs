namespace Contrib.Outbox.Core.Models;

/// <summary>
/// Result of a processing cycle of pending outbox messages.
/// </summary>
public sealed class OutboxProcessingResult
{
    public int ProcessedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }

    /// <summary>
    /// Exceptions encountered while publishing some messages of the batch.
    /// Processing of the other messages is not interrupted by an individual failure;
    /// nothing is silently swallowed, every failure is logged and surfaced here.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions { get; init; } = Array.Empty<Exception>();
}
