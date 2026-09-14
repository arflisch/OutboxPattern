using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.HealthChecks;
using Contrib.Outbox.Core.Internal;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Contrib.Outbox.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre le moteur Outbox (IOutboxEnlister, IOutboxProcessor, le Worker résident et
    /// le HealthCheck). L'application hôte doit par ailleurs enregistrer elle-même :
    /// - un IOutboxStore (via un package provider : AddMongoOutboxStore, AddSqlOutboxStore,
    ///   AddRedisOutboxStore, ou une implémentation maison),
    /// - un IOutboxDistributedLock,
    /// - un IMessagePublisher.
    /// AddOutboxCore ne dépend lui-même d'aucune techno de stockage.
    /// </summary>
    public static IServiceCollection AddOutboxCore(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));

        services.AddSingleton<OutboxWorkerState>();
        services.TryAddScoped<IOutboxEnlister, OutboxEnlister>();
        services.TryAddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();

        services.AddHealthChecks()
            .AddCheck<OutboxHealthCheck>("outbox", tags: new[] { "outbox", "ready" });

        return services;
    }

    /// <summary>
    /// Délègue au store enregistré la création de son infrastructure (index Mongo, table SQL,
    /// structures Redis...). À appeler une fois au démarrage, avant app.Run().
    /// </summary>
    public static async Task EnsureOutboxInfrastructureAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        await store.EnsureInfrastructureAsync(cancellationToken);
    }
}
