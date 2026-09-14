using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.HealthChecks;
using Contrib.Outbox.Core.Internal;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Contrib.Outbox.Core.Tests;

public class OutboxHealthCheckTests
{
    private static OutboxHealthCheck CreateSut(Mock<IOutboxStore> store, OutboxWorkerState state, OutboxOptions? options = null)
        => new(store.Object, Microsoft.Extensions.Options.Options.Create(options ?? new OutboxOptions()), state);

    [Fact]
    public async Task Unhealthy_si_le_worker_est_arrete()
    {
        var store = new Mock<IOutboxStore>();
        var state = new OutboxWorkerState();
        state.ReportStopped();

        var sut = CreateSut(store, state);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Degraded_si_des_messages_depassent_le_seuil_sla()
    {
        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.CountStaleAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(3);

        var state = new OutboxWorkerState();
        state.ReportSuccess();

        var sut = CreateSut(store, state);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(3L, result.Data["staleMessageCount"]);
    }

    [Fact]
    public async Task Degraded_si_le_dernier_cycle_a_echoue_meme_sans_message_en_retard()
    {
        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.CountStaleAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(0);

        var state = new OutboxWorkerState();
        state.ReportFailure(new InvalidOperationException("cycle en échec"));

        var sut = CreateSut(store, state);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task Healthy_si_rien_a_signaler()
    {
        var store = new Mock<IOutboxStore>();
        store.Setup(s => s.CountStaleAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(0);

        var state = new OutboxWorkerState();
        state.ReportSuccess();

        var sut = CreateSut(store, state);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Utilise_le_seuil_sla_configure()
    {
        var store = new Mock<IOutboxStore>();
        DateTime? capturedThreshold = null;

        store.Setup(s => s.CountStaleAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .Callback<DateTime, DateTime, CancellationToken>((staleBefore, _, _) => capturedThreshold = staleBefore)
             .ReturnsAsync(0);

        var state = new OutboxWorkerState();
        state.ReportSuccess();

        var sut = CreateSut(store, state, new OutboxOptions { SlaBreachThreshold = TimeSpan.FromMinutes(10) });

        var before = DateTime.UtcNow;
        await sut.CheckHealthAsync(new HealthCheckContext());
        var after = DateTime.UtcNow;

        Assert.NotNull(capturedThreshold);
        // Le seuil transmis doit être ~ (maintenant - 10 minutes), pas une valeur arbitraire.
        Assert.InRange(capturedThreshold!.Value, before.AddMinutes(-10).AddSeconds(-2), after.AddMinutes(-10).AddSeconds(2));
    }
}
