using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Models;
using Moq;
using Xunit;

namespace Contrib.Outbox.Core.Tests;

public class OutboxProcessorTests
{
    [Fact]
    public async Task Publie_les_messages_en_attente_et_les_marque_comme_envoyes()
    {
        var message = TestFactory.CreateMessage();

        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { message });

        var distributedLock = new Mock<IOutboxDistributedLock>();
        distributedLock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestFactory.CreateLockHandle($"outbox:{message.Id}").Object);

        var publisher = new Mock<IMessagePublisher>();

        var sut = TestFactory.CreateProcessor(store, distributedLock, publisher);

        var result = await sut.ProcessPendingMessagesAsync();

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Exceptions);

        publisher.Verify(p => p.PublishAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(s => s.MarkAsSentAsync(message.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ignore_un_message_deja_verrouille_par_un_autre_worker()
    {
        var message = TestFactory.CreateMessage();

        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { message });

        var distributedLock = new Mock<IOutboxDistributedLock>();
        distributedLock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IOutboxLockHandle?)null); // déjà verrouillé ailleurs

        var publisher = new Mock<IMessagePublisher>();

        var sut = TestFactory.CreateProcessor(store, distributedLock, publisher);

        var result = await sut.ProcessPendingMessagesAsync();

        Assert.Equal(0, result.ProcessedCount);
        publisher.Verify(p => p.PublishAsync(It.IsAny<StandardOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(s => s.MarkAsSentAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Un_echec_de_publication_relache_le_lock_et_continue_le_batch()
    {
        var failingMessage = TestFactory.CreateMessage();
        var succeedingMessage = TestFactory.CreateMessage();

        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { failingMessage, succeedingMessage });

        var failingHandle = TestFactory.CreateLockHandle($"outbox:{failingMessage.Id}");
        var succeedingHandle = TestFactory.CreateLockHandle($"outbox:{succeedingMessage.Id}");

        var distributedLock = new Mock<IOutboxDistributedLock>();
        distributedLock
            .Setup(l => l.TryAcquireAsync($"outbox:{failingMessage.Id}", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingHandle.Object);
        distributedLock
            .Setup(l => l.TryAcquireAsync($"outbox:{succeedingMessage.Id}", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeedingHandle.Object);

        var publisher = new Mock<IMessagePublisher>();
        publisher.Setup(p => p.PublishAsync(failingMessage, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("boom"));
        publisher.Setup(p => p.PublishAsync(succeedingMessage, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var sut = TestFactory.CreateProcessor(store, distributedLock, publisher);

        var result = await sut.ProcessPendingMessagesAsync();

        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Exceptions);
        Assert.IsType<InvalidOperationException>(result.Exceptions[0]);

        // Le lock du message en échec doit avoir été relâché (Dispose appelé par le "await using").
        failingHandle.Verify(h => h.DisposeAsync(), Times.Once);
        succeedingHandle.Verify(h => h.DisposeAsync(), Times.Once);

        store.Verify(s => s.MarkAsSentAsync(succeedingMessage.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(s => s.MarkAsSentAsync(failingMessage.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Respecte_la_taille_de_batch_demandee_au_store()
    {
        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<StandardOutboxMessage>());

        var distributedLock = new Mock<IOutboxDistributedLock>();
        var publisher = new Mock<IMessagePublisher>();

        var sut = TestFactory.CreateProcessor(
            store, distributedLock, publisher,
            options: new Options.OutboxOptions { BatchSize = 42 });

        await sut.ProcessPendingMessagesAsync();

        store.Verify(s => s.GetPendingBatchAsync(42, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
