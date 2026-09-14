using Arc4u.MongoDB;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Contrib.Outbox.Mongo.Tests;

/// <summary>
/// Contexte de test minimal : hérite d'OutboxDbContext, sans entité métier supplémentaire.
/// Constructeur sans paramètre obligatoire (contrainte new() d'AddMongoDatabase).
/// </summary>
public sealed class TestOutboxDbContext : OutboxDbContext
{
}

/// <summary>
/// Démarre un MongoDB en conteneur, configuré en replica set : les sessions/transactions
/// Mongo l'exigent, et c'est précisément ce qu'on veut tester ici.
/// La fixture est partagée par tous les tests de la collection pour ne démarrer qu'un conteneur.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithReplicaSet()
        .Build();

    public ServiceProvider Provider { get; private set; } = null!;

    public IMongoClientFactory<TestOutboxDbContext> ClientFactory =>
        Provider.GetRequiredService<IMongoClientFactory<TestOutboxDbContext>>();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();

        // Même API que dans l'application réelle, en pointant vers le conteneur.
        services.AddMongoDatabase<TestOutboxDbContext>(
            "outboxtests",
            settings =>
            {
                var fromConnectionString = MongoClientSettings
                    .FromConnectionString(_container.GetConnectionString());

                settings.Servers = fromConnectionString.Servers;
                settings.Credential = fromConnectionString.Credential;
                settings.DirectConnection = fromConnectionString.DirectConnection;
                settings.Scheme = fromConnectionString.Scheme;
                settings.ReplicaSetName = fromConnectionString.ReplicaSetName;
            });

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));

        Provider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await Provider.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Vide les collections entre deux tests.</summary>
    public async Task ResetAsync()
    {
        await ClientFactory.GetCollection<OutboxMessage>().DeleteManyAsync(FilterDefinition<OutboxMessage>.Empty);
        await ClientFactory.GetCollection<OutboxLock>().DeleteManyAsync(FilterDefinition<OutboxLock>.Empty);
    }
}

[CollectionDefinition(nameof(MongoCollection))]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>
{
}
