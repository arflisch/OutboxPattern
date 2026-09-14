using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using MongoDB.Bson.Serialization;

namespace Contrib.Outbox.Mongo;

public abstract class OutboxDbContext : DbContext
{
    protected override void OnConfiguring(DbContextBuilder context)
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
        {
            BsonClassMap.RegisterClassMap<OutboxMessage>(cm =>
            {
                cm.MapIdMember(m => m.Id).SetElementName("_id").SetIsRequired(true);
                cm.MapProperty(m => m.MessageType).SetElementName("messageType").SetIsRequired(true);
                cm.MapProperty(m => m.Payload).SetElementName("payload").SetIsRequired(true);
                cm.MapProperty(m => m.CreationTime).SetElementName("creationTime").SetIsRequired(true);
                cm.MapProperty(m => m.SentOn).SetElementName("sentOn");
                cm.MapProperty(m => m.NotBefore).SetElementName("notBefore");
                cm.MapProperty(m => m.ExpiresAt).SetElementName("expiresAt");
                cm.MapCreator(c => new OutboxMessage
                {
                    Id = c.Id,
                    MessageType = c.MessageType,
                    Payload = c.Payload,
                    CreationTime = c.CreationTime,
                    NotBefore = c.NotBefore,
                    ExpiresAt = c.ExpiresAt
                });
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxLock)))
        {
            BsonClassMap.RegisterClassMap<OutboxLock>(cm =>
            {
                cm.MapIdMember(l => l.ResourceKey).SetElementName("_id").SetIsRequired(true);
                cm.MapProperty(l => l.ExpiresAtUtc).SetElementName("expiresAtUtc").SetIsRequired(true);
                cm.MapCreator(c => new OutboxLock { ResourceKey = c.ResourceKey, ExpiresAtUtc = c.ExpiresAtUtc });
            });
        }

        context.MapCollection(OutboxMessagesCollectionName).With<OutboxMessage>();
        context.MapCollection(OutboxLocksCollectionName).With<OutboxLock>();
    }

    protected virtual string OutboxMessagesCollectionName => "OutboxMessages";
    protected virtual string OutboxLocksCollectionName => "OutboxLocks";
}
