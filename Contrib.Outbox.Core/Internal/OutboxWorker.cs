using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contrib.Outbox.Core.Internal;

internal sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly OutboxWorkerState _state;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        OutboxWorkerState state,
        ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollingInterval);

        try
        {
            do
            {
                await TickAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

            var result = await processor.ProcessPendingMessagesAsync(stoppingToken);
            _state.ReportSuccess();

            if (result.FailedCount > 0)
            {
                _logger.LogWarning(
                    "Outbox cycle completed: {Processed} processed, {Succeeded} published, {Failed} failed",
                    result.ProcessedCount, result.SucceededCount, result.FailedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during the outbox processing cycle");
            _state.ReportFailure(ex);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _state.ReportStopped();
        await base.StopAsync(cancellationToken);
    }
}
