using Contrib.Outbox.Core.Abstractions;
using Contrib.Outbox.Core.Internal;
using Contrib.Outbox.Core.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Contrib.Outbox.Core.HealthChecks;

/// <summary>
/// Database-agnostic IHealthCheck probe: Unhealthy if the BackgroundService has stopped,
/// Degraded on SLA breach (delegated to IOutboxStore.CountStaleAsync).
/// </summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly IOutboxStore _store;
    private readonly OutboxOptions _options;
    private readonly OutboxWorkerState _state;

    public OutboxHealthCheck(IOutboxStore store, IOptions<OutboxOptions> options, OutboxWorkerState state)
    {
        _store = store;
        _options = options.Value;
        _state = state;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_state.IsRunning)
        {
            return HealthCheckResult.Unhealthy("The outbox BackgroundService stopped unexpectedly.");
        }

        var now = DateTime.UtcNow;
        var staleThreshold = now - _options.SlaBreachThreshold;
        var staleCount = await _store.CountStaleAsync(staleThreshold, now, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["staleMessageCount"] = staleCount,
            ["lastSuccessfulRunUtc"] = _state.LastSuccessfulRunUtc?.ToString("O") ?? "n/a"
        };

        if (staleCount > 0)
        {
            return HealthCheckResult.Degraded(
                $"{staleCount} outbox message(s) not sent for more than {_options.SlaBreachThreshold}.",
                data: data);
        }

        if (_state.LastException is not null)
        {
            return HealthCheckResult.Degraded(
                "The last processing cycle failed but the worker keeps running.",
                _state.LastException,
                data);
        }

        return HealthCheckResult.Healthy("Outbox operational.", data);
    }
}
