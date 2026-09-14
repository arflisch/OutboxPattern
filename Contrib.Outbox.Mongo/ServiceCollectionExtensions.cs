using Contrib.Outbox.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Contrib.Outbox.Mongo;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoOutboxStore<TContext>(this IServiceCollection services)
        where TContext : OutboxDbContext
    {
        services.TryAddScoped<IOutboxStore, MongoOutboxStore<TContext>>();
        return services;
    }

    public static IServiceCollection AddMongoDistributedLock<TContext>(this IServiceCollection services)
        where TContext : OutboxDbContext
    {
        services.TryAddScoped<IOutboxDistributedLock, MongoDistributedLock<TContext>>();
        return services;
    }
}
