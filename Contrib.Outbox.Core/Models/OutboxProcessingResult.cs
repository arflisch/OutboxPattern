namespace Contrib.Outbox.Core.Models;

/// <summary>
/// Résultat d'un cycle de traitement des messages outbox en attente.
/// </summary>
public sealed class OutboxProcessingResult
{
    public int ProcessedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }

    /// <summary>
    /// Exceptions rencontrées lors de la publication de certains messages du batch.
    /// Le traitement des autres messages n'est pas interrompu par un échec individuel ;
    /// rien n'est avalé silencieusement, chaque échec est loggé et remonté ici.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions { get; init; } = Array.Empty<Exception>();
}
