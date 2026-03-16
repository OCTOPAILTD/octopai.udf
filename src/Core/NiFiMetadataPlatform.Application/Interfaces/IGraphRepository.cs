using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;

namespace NiFiMetadataPlatform.Application.Interfaces;

/// <summary>
/// Repository interface for graph operations (ArangoDB).
/// Stores ONLY column vertices and column-level lineage edges.
/// All other metadata (processors, tables, hierarchy) is stored in OpenSearch.
/// </summary>
public interface IGraphRepository
{
    /// <summary>
    /// Adds a column vertex to the graph (stores URN only).
    /// </summary>
    /// <param name="columnUrn">The column URN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddColumnVertexAsync(string columnUrn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a column lineage edge already exists.
    /// </summary>
    /// <param name="fromColumnUrn">The source column URN.</param>
    /// <param name="toColumnUrn">The target column URN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if edge exists, false otherwise.</returns>
    Task<bool> EdgeExistsAsync(
        string fromColumnUrn,
        string toColumnUrn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a column-level lineage edge (idempotent - checks before insert).
    /// </summary>
    /// <param name="fromColumnUrn">The source column URN.</param>
    /// <param name="toColumnUrn">The target column URN.</param>
    /// <param name="relationshipType">The type of relationship.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddColumnLineageEdgeAsync(
        string fromColumnUrn,
        string toColumnUrn,
        RelationshipType relationshipType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Traverses the column lineage graph.
    /// </summary>
    /// <param name="columnUrn">The starting column URN.</param>
    /// <param name="depth">The maximum depth to traverse.</param>
    /// <param name="direction">The direction of traversal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of column URNs in the lineage.</returns>
    Task<Result<List<string>>> TraverseColumnLineageAsync(
        string columnUrn,
        int depth,
        LineageDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all direct column lineage edges (depth=1) for a set of column URNs.
    /// Returns pairs of (fromColumnUrn, toColumnUrn).
    /// </summary>
    /// <param name="columnUrns">The column URNs to get edges for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of (from, to) column URN pairs.</returns>
    Task<Result<List<(string From, string To)>>> GetDirectColumnEdgesAsync(
        IEnumerable<string> columnUrns,
        CancellationToken cancellationToken = default);
}
