using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Interfaces;

/// <summary>
/// Repository interface for search operations (OpenSearch).
/// </summary>
public interface ISearchRepository
{
    /// <summary>
    /// Indexes a processor entity for search.
    /// </summary>
    /// <param name="processor">The processor entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> IndexEntityAsync(NiFiProcessor processor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a processor entity in the search index.
    /// </summary>
    /// <param name="processor">The processor entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateEntityAsync(NiFiProcessor processor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a processor entity from the search index.
    /// </summary>
    /// <param name="fqn">The processor FQN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteEntityAsync(string fqn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a processor by FQN.
    /// </summary>
    /// <param name="fqn">The processor FQN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processor entity, or null if not found.</returns>
    Task<Result<NiFiProcessor?>> GetByFqnAsync(string fqn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple processors by FQNs (bulk operation).
    /// </summary>
    /// <param name="fqns">The list of processor FQNs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of processor entities.</returns>
    Task<Result<List<NiFiProcessor>>> BulkGetAsync(
        List<string> fqns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for processors by query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="skip">Number of results to skip.</param>
    /// <param name="take">Number of results to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching processor entities.</returns>
    Task<Result<List<NiFiProcessor>>> SearchAsync(
        string query,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for processors with type and platform filters.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="typeName">Optional type filter.</param>
    /// <param name="platform">Optional platform filter.</param>
    /// <param name="count">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with matching processors and total count.</returns>
    Task<Result<(List<NiFiProcessor> Processors, int Total)>> SearchWithFiltersAsync(
        string query,
        string? typeName,
        string? platform,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets container hierarchy (containers and process groups).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of container entities.</returns>
    Task<Result<List<NiFiProcessor>>> GetHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets platform statistics (count by platform).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of platform names to counts.</returns>
    Task<Result<Dictionary<string, int>>> GetPlatformStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets children of a container entity.
    /// </summary>
    /// <param name="parentFqn">The parent container FQN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of child processor entities.</returns>
    Task<Result<List<NiFiProcessor>>> GetChildrenAsync(
        string parentFqn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a column entity for search (NiFiColumn or DatabaseColumn).
    /// </summary>
    /// <param name="column">The column entity.</param>
    /// <param name="fqn">The column FQN.</param>
    /// <param name="entityType">The entity type ("COLUMN").</param>
    /// <param name="platform">The platform (e.g., "NiFi", "MSSQL").</param>
    /// <param name="parentFqn">The parent FQN (processor or table).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> IndexColumnAsync(
        object column,
        string fqn,
        string entityType,
        string platform,
        string parentFqn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a database table entity for search.
    /// </summary>
    /// <param name="table">The table entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> IndexTableAsync(
        DatabaseTable table,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a database/schema entity for search.
    /// </summary>
    /// <param name="fqn">The FQN.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="entityType">The entity type ("DATABASE" or "SCHEMA").</param>
    /// <param name="platform">The platform (e.g., "MSSQL").</param>
    /// <param name="parentFqn">The parent FQN (optional).</param>
    /// <param name="properties">Additional properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> IndexDatabaseEntityAsync(
        string fqn,
        string name,
        string entityType,
        string platform,
        string? parentFqn,
        Dictionary<string, string> properties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets any entity by FQN as a raw AtlasEntityDto (works for processors, tables, columns, etc.).
    /// </summary>
    /// <param name="fqn">The entity FQN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity DTO, or null if not found.</returns>
    Task<Result<AtlasEntityDto?>> GetRawEntityByFqnAsync(
        string fqn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets column URNs for a processor by FQN prefix.
    /// </summary>
    /// <param name="processorFqn">The processor FQN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of column URNs.</returns>
    Task<Result<List<string>>> GetColumnUrnsByProcessorFqnAsync(
        string processorFqn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all column entities belonging to a table by table FQN.
    /// </summary>
    /// <param name="tableFqn">The table FQN (parentContainerUrn of columns).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of column DTOs with name and data type.</returns>
    Task<Result<List<ColumnInfoDto>>> GetColumnsByTableFqnAsync(
        string tableFqn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches all entity types (processors, tables, columns, schemas) returning AtlasEntityDto directly.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="typeName">Optional type filter (e.g. "TABLE", "COLUMN").</param>
    /// <param name="platform">Optional platform filter (e.g. "NiFi", "MSSQL", "Snowflake").</param>
    /// <param name="count">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with matching entities and total count.</returns>
    Task<Result<(List<AtlasEntityDto> Entities, int Total)>> SearchAllEntitiesAsync(
        string query,
        string? typeName,
        string? platform,
        int count,
        CancellationToken cancellationToken = default);
}
