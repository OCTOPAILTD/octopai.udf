using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

/// <summary>
/// Handler for SearchEntitiesQuery.
/// </summary>
public sealed class SearchEntitiesQueryHandler : IQueryHandler<SearchEntitiesQuery, Result<AtlasSearchResponse>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<SearchEntitiesQueryHandler> _logger;

    public SearchEntitiesQueryHandler(
        ISearchRepository searchRepository,
        ILogger<SearchEntitiesQueryHandler> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<Result<AtlasSearchResponse>> Handle(
        SearchEntitiesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Searching entities: Query={Query}, Type={Type}, Platform={Platform}, Count={Count}",
            request.Query,
            request.TypeName,
            request.Platform,
            request.Count);

        var result = await _searchRepository.SearchWithFiltersAsync(
            request.Query,
            request.TypeName,
            request.Platform,
            request.Count,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Search failed: {Error}", result.Error);
            return Result<AtlasSearchResponse>.Failure(result.Error!);
        }

        var (processors, total) = result.Value;

        // Map processors to DTOs
        var processorDtos = processors.Select(MapToAtlasEntity).ToList();

        // For NiFi platform, build hierarchy by adding synthetic container and process group entities
        var allResults = processorDtos;
        if (request.Platform?.Equals("NiFi", StringComparison.OrdinalIgnoreCase) == true)
        {
            allResults = BuildHierarchyWithContainers(processorDtos);
        }

        var response = new AtlasSearchResponse
        {
            Results = allResults,
            Total = total,
            Count = allResults.Count
        };

        _logger.LogInformation("Search returned {Count} results out of {Total}", response.Count, response.Total);

        return Result<AtlasSearchResponse>.Success(response);
    }

    private static AtlasEntityDto MapToAtlasEntity(NiFiProcessor processor)
    {
        return new AtlasEntityDto
        {
            Urn = processor.Fqn.Value,
            Type = "DATASET",
            Name = processor.Name.Value,
            Platform = "NiFi",
            Description = processor.Description ?? string.Empty,
            Properties = processor.Properties.ToDictionary(),
            ParentContainerUrn = processor.ParentProcessGroupId.Value
        };
    }

    private static List<AtlasEntityDto> BuildHierarchyWithContainers(List<AtlasEntityDto> processors)
    {
        var allEntities = new List<AtlasEntityDto>();
        var containersAdded = new HashSet<string>();
        var processGroupsAdded = new HashSet<string>();

        // First pass: collect all unique containers and process groups
        var containerIds = new HashSet<string>();
        var processGroupInfo = new Dictionary<string, (string containerId, string processGroupId, string processGroupName, string? parentPgUrn)>();

        foreach (var processor in processors)
        {
            var urn = processor.Urn;
            if (urn.StartsWith("nifi://container/"))
            {
                var uriPart = urn.Substring("nifi://".Length);
                var parts = uriPart.Split('/');
                
                if (parts.Length >= 3)
                {
                    var containerId = parts[1];
                    containerIds.Add(containerId);

                    // Parse the ancestor chain from processor properties (dynamic depth support)
                    if (processor.Properties != null && processor.Properties.TryGetValue("ancestorChain", out var ancestorChainJson))
                    {
                        try
                        {
                            var ancestorChain = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(ancestorChainJson);
                            
                            if (ancestorChain != null && ancestorChain.Count > 0)
                            {
                                // Build process group entities for each ancestor in the chain
                                string? previousPgUrn = null;
                                
                                for (int i = 0; i < ancestorChain.Count; i++)
                                {
                                    var ancestor = ancestorChain[i];
                                    if (ancestor.TryGetValue("id", out var pgId) && ancestor.TryGetValue("name", out var pgName))
                                    {
                                        var pgUrn = $"nifi://container/{containerId}/process-group/{pgId}";
                                        
                                        if (!processGroupInfo.ContainsKey(pgUrn))
                                        {
                                            // Determine parent: first ancestor's parent is container, others point to previous ancestor
                                            var parentUrn = i == 0 ? null : previousPgUrn;
                                            processGroupInfo[pgUrn] = (containerId, pgId, pgName, parentUrn);
                                        }
                                        
                                        previousPgUrn = pgUrn;
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Fallback: ignore if JSON parsing fails
                        }
                    }
                }
            }
        }

        // Second pass: create container entities
        foreach (var containerId in containerIds)
        {
            var containerUrn = $"nifi://container/{containerId}";
            allEntities.Add(new AtlasEntityDto
            {
                Urn = containerUrn,
                Type = "CONTAINER",
                Name = $"nifi-flow",
                Platform = "NiFi",
                Description = "NiFi container instance",
                Properties = new Dictionary<string, string> { { "containerId", containerId } },
                ParentContainerUrn = null
            });
        }

        // Third pass: create process group entities
        foreach (var (pgUrn, (containerId, processGroupId, processGroupName, parentPgUrn)) in processGroupInfo)
        {
            var containerUrn = $"nifi://container/{containerId}";

            allEntities.Add(new AtlasEntityDto
            {
                Urn = pgUrn,
                Type = "CONTAINER",
                Name = processGroupName,
                Platform = "NiFi",
                Description = "NiFi process group",
                Properties = new Dictionary<string, string> { { "processGroupId", processGroupId } },
                ParentContainerUrn = parentPgUrn ?? containerUrn
            });
        }

        // Fourth pass: add all processors
        allEntities.AddRange(processors);

        return allEntities;
    }
}
