using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Contrib.Outbox.Mongo.Tests;

[Collection(nameof(MongoCollection))]
public sealed class MongoOutboxStoreTests : IAsyncLifetime
{
    private readonly MongoFixture _fixture;
    private readonly MongoOutboxStore<TestOutboxDbContext> _sut;

    public MongoOutboxStoreTests(MongoFixture fixture)
    {
        _fixture = fixture;
        _sut = new MongoOutboxStore<TestOutboxDbContext>(
            fixture.ClientFactory, Options.Create(new OutboxOptions()));
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record TestEvent(string Value) : IEvent;

    [Fact]
    public async Task EnsureInfrastructure_cree_les_index_polling_et_ttl()
    {
        await _sut.EnsureInfrastructureAsync();

        var indexes = await (await _fixture.ClientFactory.GetCollection<OutboxMessage>().Indexes.ListAsync())
            .ToListAsync();
        var names = indexes.Select(i => i["name"].AsString).ToList();

        Assert.Contains("ix_outbox_polling", names);
        Assert.Contains("ix_outbox_ttl", names);

        // L'index TTL doit bien porter une expiration, sinon la purge automatique ne se fait pas.
        var ttl = indexes.Single(i => i["name"].AsString == "ix_outbox_ttl");
        Assert.True(ttl.Contains("expireAfterSeconds"));
    }

    [Fact]
    public async Task Enlist_hors_transaction_persiste_le_message()
    {
        await _sut.EnlistAsync(new TestEvent("hello"), transactionContext: null);

        var pending = await _sut.GetPendingBatchAsync(10, DateTime.UtcNow);

        Assert.Single(pending);
        Assert.Contains("hello", pending[0].Payload);
    }

    [Fact]
    public async Task Enlist_dans_une_transaction_commitee_persiste_le_message()
    {
        var client = _fixture.ClientFactory.CreateClient();
        using var session = await client.StartSessionAsync();
        session.StartTransaction();

        await _sut.EnlistAsync(new TestEvent("commit"), session);
        await session.CommitTransactionAsync();

        var pending = await _sut.GetPendingBatchAsync(10, DateTime.UtcNow);
        Assert.Single(pending);
    }

    [Fact]
    public async Task Enlist_dans_une_transaction_avortee_ne_persiste_rien()
    {
        var client = _fixture.ClientFactory.CreateClient();
        using var session = await client.StartSessionAsync();
        session.StartTransaction();

        await _sut.EnlistAsync(new TestEvent("rollback"), session);
        await session.AbortTransactionAsync();

        var pending = await _sut.GetPendingBatchAsync(10, DateTime.UtcNow);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Enlist_rejette_un_transactionContext_de_mauvais_type()
    {
        await Assert.ThrowsAsync<InvalidCastException>(
            () => _sut.EnlistAsync(new TestEvent("x"), transactionContext: "pas une session"));
    }

    [Fact]
    public async Task GetPendingBatch_exclut_les_messages_deja_envoyes()
    {
        await _sut.EnlistAsync(new TestEvent("sent"), null);
        var pending = await _sut.GetPendingBatchAsync(10, DateTime.UtcNow);

        await _sut.MarkAsSentAsync(pending[0].Id, DateTime.UtcNow);

        Assert.Empty(await _sut.GetPendingBatchAsync(10, DateTime.UtcNow));
    }

    [Fact]
    public async Task GetPendingBatch_exclut_les_messages_dont_NotBefore_est_dans_le_futur()
    {
        await InsertRawAsync(notBefore: DateTime.UtcNow.AddHours(1));

        Assert.Empty(await _sut.GetPendingBatchAsync(10, DateTime.UtcNow));
        // Mais devient éligible une fois la date atteinte.
        Assert.Single(await _sut.GetPendingBatchAsync(10, DateTime.UtcNow.AddHours(2)));
    }

    [Fact]
    public async Task GetPendingBatch_exclut_les_messages_expires()
    {
        await InsertRawAsync(expiresAt: DateTime.UtcNow.AddHours(-1));

        Assert.Empty(await _sut.GetPendingBatchAsync(10, DateTime.UtcNow));
    }

    [Fact]
    public async Task GetPendingBatch_respecte_la_taille_et_trie_du_plus_ancien_au_plus_recent()
    {
        var oldest = DateTime.UtcNow.AddMinutes(-10);
        await InsertRawAsync(creationTime: DateTime.UtcNow.AddMinutes(-1), payload: "recent");
        await InsertRawAsync(creationTime: oldest, payload: "ancien");
        await InsertRawAsync(creationTime: DateTime.UtcNow.AddMinutes(-5), payload: "milieu");

        var batch = await _sut.GetPendingBatchAsync(2, DateTime.UtcNow);

        Assert.Equal(2, batch.Count);
        Assert.Contains("ancien", batch[0].Payload);
        Assert.Contains("milieu", batch[1].Payload);
    }

    [Fact]
    public async Task CountStale_ne_compte_que_les_messages_non_envoyes_et_assez_anciens()
    {
        await InsertRawAsync(creationTime: DateTime.UtcNow.AddMinutes(-10)); // en retard
        await InsertRawAsync(creationTime: DateTime.UtcNow);                 // récent

        var staleBefore = DateTime.UtcNow.AddMinutes(-5);
        Assert.Equal(1, await _sut.CountStaleAsync(staleBefore, DateTime.UtcNow));
    }

    private Task InsertRawAsync(
        DateTime? creationTime = null,
        DateTime? notBefore = null,
        DateTime? expiresAt = null,
        string payload = "{}")
    {
        var document = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestEvent",
            Payload = payload,
            CreationTime = creationTime ?? DateTime.UtcNow,
            NotBefore = notBefore,
            ExpiresAt = expiresAt
        };

        return _fixture.ClientFactory.GetCollection<OutboxMessage>().InsertOneAsync(document);
    }
}
