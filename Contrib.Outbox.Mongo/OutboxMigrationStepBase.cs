using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mongrow.Steps;

namespace Contrib.Outbox.Mongo;

public abstract class OutboxMigrationStepBase
{
    private readonly OutboxOptions _options;

    protected OutboxMigrationStepBase(IOptions<OutboxOptions> options)
    {
        _options = options.Value;
    }

    protected virtual string OutboxMessagesCollectionName => "OutboxMessages";
    protected virtual string OutboxLocksCollectionName => "OutboxLocks";

    protected async Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        await EnsureCollectionExistsAsync(database, OutboxMessagesCollectionName, cancellationToken);
        await EnsureCollectionExistsAsync(database, OutboxLocksCollectionName, cancellationToken);

        var messages = database.GetCollection<BsonDocument>(OutboxMessagesCollectionName);

        var pollingIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys
                .Ascending("sentOn")
                .Ascending("creationTime")
                .Ascending("notBefore"),
            new CreateIndexOptions { Name = "ix_outbox_polling" });

        var ttlIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("sentOn"),
            new CreateIndexOptions { Name = "ix_outbox_ttl", ExpireAfter = _options.SentMessageRetention });

        await messages.Indexes.CreateManyAsync(new[] { pollingIndex, ttlIndex }, cancellationToken);
    }

    private static async Task EnsureCollectionExistsAsync(
        IMongoDatabase database, string collectionName, CancellationToken cancellationToken)
    {
        var filter = new BsonDocument("name", collectionName);
        var cursor = await database.ListCollectionNamesAsync(
            new ListCollectionNamesOptions { Filter = filter }, cancellationToken);
        var existing = await cursor.ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        }
    }
}