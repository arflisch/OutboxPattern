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
    /// Registers the Outbox engine (IOutboxEnlister, IOutboxProcessor, the resident Worker and
    /// the HealthCheck). The host application must additionally register on its own:
    /// - an IOutboxStore (via a provider package: AddMongoOutboxStore, AddSqlOutboxStore,
    ///   AddRedisOutboxStore, or a custom implementation),
    /// - an IOutboxDistributedLock,
    /// - an IMessagePublisher.
    /// AddOutboxCore itself does not depend on any storage technology.
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
    /// Delegates to the registered store the creation of its infrastructure (Mongo indexes, SQL table,
    /// Redis structures...). Call once at startup, before app.Run().
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
