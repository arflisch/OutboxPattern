using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Extensions;
using Contrib.Outbox.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Contrib.Outbox.Core.Tests;

public class ServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithFakes(Mock<IOutboxStore>? store = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton((store ?? new Mock<IOutboxStore>()).Object);
        services.AddSingleton(new Mock<IOutboxDistributedLock>().Object);
        services.AddSingleton(new Mock<IMessagePublisher>().Object);
        return services;
    }
    
    private sealed record TestEvent(string OrderId) : IEvent;

    [Fact]
    public void AddOutboxCore_enregistre_IOutboxEnlister_et_IOutboxProcessor()
    {
        var services = CreateServicesWithFakes();
        services.AddOutboxCore();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOutboxEnlister>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOutboxProcessor>());
    }

    [Fact]
    public void AddOutboxCore_enregistre_le_worker_comme_hosted_service()
    {
        var services = CreateServicesWithFakes();
        services.AddOutboxCore();

        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s.GetType().Name == "OutboxWorker");
    }

    [Fact]
    public async Task IOutboxEnlister_delegue_bien_vers_IOutboxStore()
    {
        var store = new Mock<IOutboxStore>();
        var services = CreateServicesWithFakes(store);
        services.AddOutboxCore();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var enlister = scope.ServiceProvider.GetRequiredService<IOutboxEnlister>();
        var message = new TestEvent("order-123");

        await enlister.EnlistAsync(message, transactionContext: null);

        store.Verify(s => s.EnlistAsync(message, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureOutboxInfrastructureAsync_delegue_vers_le_store()
    {
        var store = new Mock<IOutboxStore>();
        var services = CreateServicesWithFakes(store);
        services.AddOutboxCore();

        using var provider = services.BuildServiceProvider();

        await provider.EnsureOutboxInfrastructureAsync();

        store.Verify(s => s.EnsureInfrastructureAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
