using Arc4u.MongoDB;
using Contrib.Outbox.Core.Abstractions;
using MongoDB.Driver;

namespace Contrib.Outbox.Mongo;

public sealed class MongoDistributedLock<TContext> : IOutboxDistributedLock
    where TContext : OutboxDbContext
{
    private readonly IMongoClientFactory<TContext> _clientFactory;

    public MongoDistributedLock(IMongoClientFactory<TContext> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    private IMongoCollection<OutboxLock> Locks => _clientFactory.GetCollection<OutboxLock>();

    public async Task<IOutboxLockHandle?> TryAcquireAsync(
        string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var replacement = new OutboxLock { ResourceKey = resourceKey, ExpiresAtUtc = now + timeout };
        var collection = Locks;

        try
        {
            await collection.InsertOneAsync(replacement, cancellationToken: cancellationToken);
            return new MongoLockHandle(collection, resourceKey);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var filter = Builders<OutboxLock>.Filter.And(
                Builders<OutboxLock>.Filter.Eq(l => l.ResourceKey, resourceKey),
                Builders<OutboxLock>.Filter.Lt(l => l.ExpiresAtUtc, now));

            var result = await collection.ReplaceOneAsync(filter, replacement, cancellationToken: cancellationToken);
            return result.ModifiedCount == 1 ? new MongoLockHandle(collection, resourceKey) : null;
        }
    }

    private sealed class MongoLockHandle : IOutboxLockHandle
    {
        private readonly IMongoCollection<OutboxLock> _collection;
        public string ResourceKey { get; }

        public MongoLockHandle(IMongoCollection<OutboxLock> collection, string resourceKey)
        {
            _collection = collection;
            ResourceKey = resourceKey;
        }

        public async ValueTask DisposeAsync()
        {
            await _collection.DeleteOneAsync(l => l.ResourceKey == ResourceKey);
        }
    }
}
