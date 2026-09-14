namespace Contrib.Outbox.Core.Options;

/// <summary>
/// Options du moteur, communes à tous les providers. Les options spécifiques à un stockage
/// (nom de collection Mongo, nom de table SQL, préfixe de clé Redis, ...) vivent dans les
/// classes d'options propres à chaque package provider (ex: MongoOutboxOptions).
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Nombre max de messages traités par cycle de polling.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Fréquence du PeriodicTimer du Worker.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Timeout d'acquisition du lock distribué par message.
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Durée de rétention des messages déjà envoyés avant purge (portée par le provider :
    /// index TTL Mongo, job de purge SQL, expiration Redis, ...).
    /// </summary>
    public TimeSpan SentMessageRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Seuil au-delà duquel un message non envoyé déclenche un statut Degraded du HealthCheck.
    /// </summary>
    public TimeSpan SlaBreachThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
