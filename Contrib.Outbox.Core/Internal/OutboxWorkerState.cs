namespace Contrib.Outbox.Core.Internal;

/// <summary>
/// State shared between the BackgroundService and the HealthCheck (singleton).
/// </summary>
public sealed class OutboxWorkerState
{
    private volatile bool _isRunning = true;
    private DateTimeOffset? _lastSuccessfulRunUtc;
    private Exception? _lastException;

    public bool IsRunning => _isRunning;
    public DateTimeOffset? LastSuccessfulRunUtc => _lastSuccessfulRunUtc;
    public Exception? LastException => _lastException;

    internal void ReportSuccess()
    {
        _lastSuccessfulRunUtc = DateTimeOffset.UtcNow;
        _lastException = null;
    }

    internal void ReportFailure(Exception exception) => _lastException = exception;

    internal void ReportStopped() => _isRunning = false;
}
