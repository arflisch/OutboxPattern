namespace Contrib.Outbox.Core.Models;

/// <summary>
/// Représentation d'un message outbox, indépendante de tout moteur de stockage.
/// Chaque provider (Mongo, Sql, Redis...) est responsable de mapper ce modèle
/// vers son propre format de persistance (document, ligne, hash...).
/// </summary>
public sealed class StandardOutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Nom qualifié du type du message, utilisé pour la (dé)sérialisation du Payload.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Payload sérialisé (JSON) du message métier.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date d'envoi effectif. Null tant que le message est en attente.
    /// </summary>
    public DateTime? SentOn { get; set; }

    /// <summary>
    /// Envoi différé : le message n'est éligible au polling qu'à partir de cette date.
    /// </summary>
    public DateTime? NotBefore { get; init; }

    /// <summary>
    /// Time-to-live applicatif : passé cette date, le message n'est plus publié.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
