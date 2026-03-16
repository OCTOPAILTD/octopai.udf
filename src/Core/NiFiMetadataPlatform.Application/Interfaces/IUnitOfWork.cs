namespace NiFiMetadataPlatform.Application.Interfaces;

/// <summary>
/// Unit of work for coordinating transactions across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the graph repository.
    /// </summary>
    IGraphRepository GraphRepository { get; }

    /// <summary>
    /// Gets the search repository.
    /// </summary>
    ISearchRepository SearchRepository { get; }

    /// <summary>
    /// Begins a transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
