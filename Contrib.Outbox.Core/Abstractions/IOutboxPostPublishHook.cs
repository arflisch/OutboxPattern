using Contrib.Outbox.Core.Models;

namespace Contrib.Outbox.Core.Abstractions;

/// <summary>
/// Hook appelé immédiatement après chaque publication réussie (ex: pour redéclencher
/// un pattern Visitor existant côté application).
/// </summary>
public interface IOutboxPostPublishHook
{
    Task OnPublishedAsync(StandardOutboxMessage message, CancellationToken cancellationToken = default);
}
