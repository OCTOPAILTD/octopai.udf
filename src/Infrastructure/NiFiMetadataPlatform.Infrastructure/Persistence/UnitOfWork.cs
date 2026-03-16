using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Common;

namespace NiFiMetadataPlatform.Infrastructure.Persistence;

/// <summary>
/// Unit of work implementation for coordinating transactions.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISearchRepository _searchRepository;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="graphRepository">The graph repository.</param>
    /// <param name="searchRepository">The search repository.</param>
    public UnitOfWork(
        IGraphRepository graphRepository,
        ISearchRepository searchRepository)
    {
        _graphRepository = graphRepository;
        _searchRepository = searchRepository;
    }

    /// <inheritdoc/>
    public IGraphRepository GraphRepository => _graphRepository;

    /// <inheritdoc/>
    public ISearchRepository SearchRepository => _searchRepository;

    /// <inheritdoc/>
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // For now, we don't have explicit transactions
        // OpenSearch and ArangoDB handle their own transactions
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        // For now, we don't have explicit transactions
        // OpenSearch and ArangoDB auto-commit
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc/>
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        // For now, we don't have explicit rollback
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
