using System.Text.Json;
using Arc4u.MongoDB;
using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Models;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Contrib.Outbox.Mongo;

public sealed class MongoOutboxStore<TContext> : IOutboxStore
    where TContext : OutboxDbContext
{
    private readonly IMongoClientFactory<TContext> _clientFactory;
    private readonly OutboxOptions _engineOptions;

    public MongoOutboxStore(IMongoClientFactory<TContext> clientFactory, IOptions<OutboxOptions> engineOptions)
    {
        _clientFactory = clientFactory;
        _engineOptions = engineOptions.Value;
    }
    
    private IMongoCollection<OutboxMessage> OutboxMessages => _clientFactory.GetCollection<OutboxMessage>();

    public async Task EnlistAsync<T>(T @event, object? transactionContext, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        var document = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = @event.GetType().Name,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            CreationTime = DateTime.UtcNow
        };

        if (transactionContext is null)
        {
            await OutboxMessages.InsertOneAsync(document, cancellationToken: cancellationToken);
            return;
        }

        if (transactionContext is not IClientSessionHandle session)
        {
            throw new InvalidCastException(
                $"MongoOutboxStore expects an IClientSessionHandle as transactionContext, received: {transactionContext.GetType()}");
        }

        await OutboxMessages.InsertOneAsync(session, document, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<StandardOutboxMessage>> GetPendingBatchAsync(
        int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(m => m.SentOn, null),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(m => m.NotBefore, null),
                Builders<OutboxMessage>.Filter.Lte(m => m.NotBefore, nowUtc)),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(m => m.ExpiresAt, null),
                Builders<OutboxMessage>.Filter.Gt(m => m.ExpiresAt, nowUtc)));

        var documents = await OutboxMessages.Find(filter)
            .Sort(Builders<OutboxMessage>.Sort.Ascending(m => m.CreationTime))
            .Limit(batchSize)
            .ToListAsync(cancellationToken);

        return documents.Select(ToModel).ToList();
    }

    public Task MarkAsSentAsync(Guid messageId, DateTime sentOnUtc, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessage>.Update.Set(m => m.SentOn, sentOnUtc);
        return OutboxMessages.UpdateOneAsync(m => m.Id == messageId, update, cancellationToken: cancellationToken);
    }

    public async Task<long> CountStaleAsync(
        DateTime staleBeforeUtc, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(m => m.SentOn, null),
            Builders<OutboxMessage>.Filter.Lt(m => m.CreationTime, staleBeforeUtc),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(m => m.ExpiresAt, null),
                Builders<OutboxMessage>.Filter.Gt(m => m.ExpiresAt, nowUtc)));

        return await OutboxMessages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task EnsureInfrastructureAsync(CancellationToken cancellationToken = default)
    {
        var pollingIndexModel = new CreateIndexModel<OutboxMessage>(
            Builders<OutboxMessage>.IndexKeys
                .Ascending(m => m.SentOn)
                .Ascending(m => m.CreationTime)
                .Ascending(m => m.NotBefore),
            new CreateIndexOptions { Name = "ix_outbox_polling" });

        var ttlIndexModel = new CreateIndexModel<OutboxMessage>(
            Builders<OutboxMessage>.IndexKeys.Ascending(m => m.SentOn),
            new CreateIndexOptions { Name = "ix_outbox_ttl", ExpireAfter = _engineOptions.SentMessageRetention });

        await OutboxMessages.Indexes.CreateManyAsync(new[] { pollingIndexModel, ttlIndexModel }, cancellationToken);
    }

    private static StandardOutboxMessage ToModel(OutboxMessage m) => new()
    {
        Id = m.Id,
        MessageType = m.MessageType,
        Payload = m.Payload,
        CreationTime = m.CreationTime,
        SentOn = m.SentOn,
        NotBefore = m.NotBefore,
        ExpiresAt = m.ExpiresAt
    };
}
