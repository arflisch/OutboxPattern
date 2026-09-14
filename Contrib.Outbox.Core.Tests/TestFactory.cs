using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Internal;
using Contrib.Outbox.Core.Models;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Contrib.Outbox.Core.Tests;

internal static class TestFactory
{
    public static OutboxProcessor CreateProcessor(
        Mock<IOutboxStore> store,
        Mock<IOutboxDistributedLock> distributedLock,
        Mock<IMessagePublisher> publisher,
        IOutboxPostPublishHook? hook = null,
        OutboxOptions? options = null)
    {
        return new OutboxProcessor(
            store.Object,
            distributedLock.Object,
            publisher.Object,
            Microsoft.Extensions.Options.Options.Create(options ?? new OutboxOptions()),
            NullLogger<OutboxProcessor>.Instance,
            hook);
    }

    public static StandardOutboxMessage CreateMessage(DateTime? creationTime = null) => new()
    {
        Id = Guid.NewGuid(),
        MessageType = "TestMessage",
        Payload = "{}",
        CreationTime = creationTime ?? DateTime.UtcNow
    };

    public static Mock<IOutboxLockHandle> CreateLockHandle(string resourceKey)
    {
        var handle = new Mock<IOutboxLockHandle>();
        handle.SetupGet(h => h.ResourceKey).Returns(resourceKey);
        return handle;
    }
}
