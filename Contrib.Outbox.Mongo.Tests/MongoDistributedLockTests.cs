using Xunit;

namespace Contrib.Outbox.Mongo.Tests;

[Collection(nameof(MongoCollection))]
public sealed class MongoDistributedLockTests : IAsyncLifetime
{
    private readonly MongoFixture _fixture;
    private readonly MongoDistributedLock<TestOutboxDbContext> _sut;

    public MongoDistributedLockTests(MongoFixture fixture)
    {
        _fixture = fixture;
        _sut = new MongoDistributedLock<TestOutboxDbContext>(fixture.ClientFactory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Acquiert_un_lock_libre()
    {
        await using var handle = await _sut.TryAcquireAsync("outbox:1", TimeSpan.FromSeconds(30));

        Assert.NotNull(handle);
        Assert.Equal("outbox:1", handle!.ResourceKey);
    }

    [Fact]
    public async Task Refuse_un_lock_deja_detenu()
    {
        await using var first = await _sut.TryAcquireAsync("outbox:2", TimeSpan.FromSeconds(30));
        Assert.NotNull(first);

        var second = await _sut.TryAcquireAsync("outbox:2", TimeSpan.FromSeconds(30));
        Assert.Null(second);
    }

    [Fact]
    public async Task Libere_le_lock_au_dispose_et_permet_une_nouvelle_acquisition()
    {
        var first = await _sut.TryAcquireAsync("outbox:3", TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        await first!.DisposeAsync();

        await using var second = await _sut.TryAcquireAsync("outbox:3", TimeSpan.FromSeconds(30));
        Assert.NotNull(second);
    }

    [Fact]
    public async Task Reprend_un_lock_expire()
    {
        // Lock déjà expiré (timeout négatif) : un autre worker doit pouvoir le reprendre.
        await _sut.TryAcquireAsync("outbox:4", TimeSpan.FromSeconds(-30));

        await using var handle = await _sut.TryAcquireAsync("outbox:4", TimeSpan.FromSeconds(30));
        Assert.NotNull(handle);
    }

    [Fact]
    public async Task Un_seul_gagnant_en_cas_d_acquisitions_concurrentes()
    {
        // Simule plusieurs workers qui tombent sur le même message en même temps.
        var attempts = Enumerable.Range(0, 10)
            .Select(_ => _sut.TryAcquireAsync("outbox:concurrent", TimeSpan.FromSeconds(30)));

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(r => r is not null));
    }
}
