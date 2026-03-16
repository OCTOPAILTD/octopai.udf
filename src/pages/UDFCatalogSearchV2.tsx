import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Search,
  Database,
  GitBranch,
  FileText,
  Filter,
  ChevronDown,
  ChevronRight,
  X,
  Loader2,
  FolderTree,
  Server,
  Table2,
  Layers,
} from 'lucide-react';
import config from '../config';

interface SearchResult {
  urn: string;
  type: string;
  name: string;
  platform?: string;
  description?: string;
  properties?: Record<string, string>;
  children?: SearchResult[];
  isExpanded?: boolean;
  level?: number;
  parentUrn?: string;
}

const UDFCatalogSearchV2 = () => {
  // V2: Using Atlas API - Fresh component to bypass cache issues
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [searchQuery, setSearchQuery] = useState(searchParams.get('q') || '');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [selectedType, setSelectedType] = useState<string | null>(searchParams.get('type'));
  const [selectedPlatform, setSelectedPlatform] = useState<string | null>(searchParams.get('platform'));
  const [showTypeFilter, setShowTypeFilter] = useState(false);
  const [showPlatformFilter, setShowPlatformFilter] = useState(false);

  const entityTypes = [
    { value: 'DATASET', label: 'Dataset', icon: Database },
    { value: 'TABLE', label: 'Table', icon: Table2 },
    { value: 'DATA_FLOW', label: 'Data Flow', icon: GitBranch },
    { value: 'DATA_JOB', label: 'Data Job', icon: FileText },
    { value: 'DATABASE', label: 'Database', icon: Server },
    { value: 'SCHEMA', label: 'Schema', icon: Layers },
  ];

  const platforms = ['NiFi', 'MSSQL', 'Snowflake', 'File', 'Kafka', 'Hive', 'Spark'];

  useEffect(() => {
    const q = searchParams.get('q');
    const type = searchParams.get('type');
    const platform = searchParams.get('platform');
    
    if (q) setSearchQuery(q);
    if (type) setSelectedType(type);
    if (platform) setSelectedPlatform(platform);

    if (q || type || platform) {
      performSearch(q || '*', type, platform);
    }
  }, [searchParams]);

  const performSearch = async (query: string, type: string | null = null, platform: string | null = null) => {
    try {
      setLoading(true);

      // Use Atlas API (UPDATED TO USE ATLAS)
      console.log('[UDFCatalogSearch] Using Atlas API - Version 2.0');
      let url = `${config.backendUrl}/api/atlas/search?query=${encodeURIComponent(query || '*')}&count=50`;
      
      if (type) {
        url += `&type_name=${type}`;
      }
      
      if (platform) {
        url += `&platform=${encodeURIComponent(platform)}`;
      }

      console.log('[UDFCatalogSearch] Atlas API URL:', url);
      const response = await fetch(url);
      
      if (!response.ok) {
        throw new Error(`Failed to search entities: ${response.statusText}`);
      }

      const data = await response.json();
      console.log('[UDFCatalogSearch] Response:', { total: data.total, count: data.count, resultsLength: data.results?.length });
      console.log('[UDFCatalogSearch] Results by type:', data.results?.reduce((acc: any, r: any) => { acc[r.type] = (acc[r.type] || 0) + 1; return acc; }, {}));

      const entities = data.results.map((entity: any) => ({
        urn: entity.urn,
        type: entity.type,
        name: entity.name || extractNameFromUrn(entity.urn),
        platform: entity.platform || 'Unknown',
        description: entity.description || '',
        properties: entity.properties || {},
        parent_container_urn: entity.parentContainerUrn || entity.parent_container_urn,  // API returns camelCase
      }));

      console.log('[UDFCatalogSearch] Mapped entities:', entities.length, 'items');
      console.log('[UDFCatalogSearch] Sample entity with parent_container_urn:', entities.find((e: any) => e.parent_container_urn));
      console.log('[UDFCatalogSearch] Entities by type:', entities.reduce((acc: any, e: any) => {
        acc[e.type] = (acc[e.type] || 0) + 1;
        return acc;
      }, {}));
      
      // Build hierarchy for NiFi platform
      const hierarchicalResults = platform?.toLowerCase() === 'nifi' 
        ? buildHierarchy(entities)
        : entities.map(e => ({ ...e, level: 0 }));
      
      setResults(hierarchicalResults);
      setTotalCount(data.total);
    } catch (error) {
      console.error('Search failed:', error);
      setResults([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  const extractNameFromUrn = (urn: string): string => {
    const parts = urn.split(':');
    return parts[parts.length - 1] || urn;
  };

  const buildHierarchy = (flatResults: SearchResult[]): SearchResult[] => {
    console.log('[buildHierarchy] Starting with', flatResults.length, 'items');
    console.log('[buildHierarchy] All items:', flatResults.map(r => ({
      name: r.name,
      urn: r.urn,
      parent_container_urn: r.parent_container_urn
    })));
    
    // Build hierarchy using parent_container_urn
    const hierarchy: SearchResult[] = [];
    const itemsByUrn = new Map<string, SearchResult>();
    
    // Index all items by URN
    for (const item of flatResults) {
      itemsByUrn.set(item.urn, { ...item, children: [], level: 0 });
    }
    
    console.log('[buildHierarchy] Indexed URNs:', Array.from(itemsByUrn.keys()));
    
    // Build parent-child relationships
    for (const item of itemsByUrn.values()) {
      const parentUrn = item.parent_container_urn;
      
      if (parentUrn) {
        if (itemsByUrn.has(parentUrn)) {
          const parent = itemsByUrn.get(parentUrn)!;
          item.level = (parent.level || 0) + 1;
          item.parentUrn = parentUrn;
          if (!parent.children) parent.children = [];
          parent.children.push(item);
          console.log(`[buildHierarchy] ✅ Linked ${item.name} to parent ${parent.name}`);
        } else {
          // Parent URN not found in map
          hierarchy.push(item);
          console.log(`[buildHierarchy] ❌ Parent not found for ${item.name}, parent URN: ${parentUrn}`);
        }
      } else {
        // No parent, this is a root item
        hierarchy.push(item);
        console.log(`[buildHierarchy] 🔹 Root item: ${item.name} (no parent)`);
      }
    }
    
    // Debug logging
    console.log('[buildHierarchy] Root items:', hierarchy.length);
    console.log('[buildHierarchy] Hierarchy structure:', hierarchy.map(h => ({
      name: h.name,
      type: h.type,
      children: h.children?.length || 0,
      childNames: h.children?.map(c => c.name) || []
    })));
    
    // If no hierarchy was built, return flat list
    if (hierarchy.length === 0) {
      console.log('[buildHierarchy] No hierarchy built, returning flat list');
      return flatResults.map(r => ({ ...r, level: 0, children: [] }));
    }
    
    return hierarchy;
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (searchQuery) params.set('q', searchQuery);
    if (selectedType) params.set('type', selectedType);
    if (selectedPlatform) params.set('platform', selectedPlatform);
    navigate(`/udf-catalog/search?${params.toString()}`);
  };

  const handleEntityClick = (urn: string) => {
    navigate(`/udf-catalog/entity/${encodeURIComponent(urn)}`);
  };

  const clearFilters = () => {
    setSelectedType(null);
    setSelectedPlatform(null);
    navigate(`/udf-catalog/search?q=${searchQuery}`);
  };

  const getEntityIcon = (type: string, urn?: string) => {
    const typeUpper = type.toUpperCase();
    if (typeUpper === 'CONTAINER') return urn?.includes('nifi-instance-') ? Server : FolderTree;
    if (typeUpper === 'TABLE') return Table2;
    if (typeUpper === 'DATABASE') return Server;
    if (typeUpper === 'SCHEMA') return Layers;
    const entityType = entityTypes.find(t => t.value === typeUpper);
    return entityType?.icon || Database;
  };

  const getPlatformBadgeClass = (platform: string) => {
    switch (platform?.toLowerCase()) {
      case 'mssql': return 'bg-red-100 text-red-700';
      case 'snowflake': return 'bg-blue-100 text-blue-700';
      case 'nifi': return 'bg-orange-100 text-orange-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  };

  const getHierarchyBreadcrumb = (urn: string): string[] => {
    // jdbc://server/database/schema/table → [server, database, schema, table]
    if (urn.startsWith('jdbc://')) {
      const parts = urn.replace('jdbc://', '').split('/').filter(Boolean);
      return parts;
    }
    return [];
  };

  const hasActiveFilters = selectedType || selectedPlatform;

  // TreeNode component for hierarchical display
  const TreeNode = ({ node, onNavigate }: { node: SearchResult; onNavigate: (urn: string) => void }) => {
    const [isExpanded, setIsExpanded] = useState((node.level || 0) <= 1);
    const hasChildren = node.children && node.children.length > 0;
    const Icon = getEntityIcon(node.type, node.urn);
    const level = node.level || 0;
    const isTable = node.type?.toUpperCase() === 'TABLE';
    const breadcrumb = isTable ? getHierarchyBreadcrumb(node.urn) : [];

    const handleClick = (e: React.MouseEvent) => {
      if (hasChildren) {
        e.stopPropagation();
        setIsExpanded(!isExpanded);
      } else {
        onNavigate(node.urn);
      }
    };

    return (
      <div>
        <button
          onClick={handleClick}
          className="w-full bg-white rounded-lg border border-gray-200 p-5 hover:shadow-md hover:border-blue-400 transition-all text-left"
          style={{ marginLeft: level > 0 ? `${level * 2}rem` : '0' }}
        >
          <div className="flex items-start gap-4">
            {hasChildren ? (
              <div className="flex-shrink-0 w-6 h-6 flex items-center justify-center mt-1">
                <ChevronRight className={`w-5 h-5 text-gray-400 transform transition-transform ${isExpanded ? 'rotate-90' : ''}`} />
              </div>
            ) : (
              <div className="w-6 flex-shrink-0" />
            )}
            <div className={`w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 ${isTable ? 'bg-purple-50' : 'bg-blue-50'}`}>
              <Icon className={`w-5 h-5 ${isTable ? 'text-purple-600' : 'text-blue-600'}`} />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap mb-1">
                <h3 className="font-semibold text-gray-900 text-base">{node.name}</h3>
                <span className={`px-2 py-0.5 text-xs font-medium rounded ${isTable ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'}`}>
                  {node.type}
                </span>
                {node.platform && (
                  <span className={`px-2 py-0.5 text-xs font-medium rounded ${getPlatformBadgeClass(node.platform)}`}>
                    {node.platform}
                  </span>
                )}
                {hasChildren && (
                  <span className="px-2 py-0.5 bg-green-100 text-green-700 text-xs font-medium rounded">
                    {node.children!.length} item{node.children!.length !== 1 ? 's' : ''}
                  </span>
                )}
              </div>
              {/* Hierarchy breadcrumb for TABLE entities */}
              {breadcrumb.length > 1 && (
                <div className="flex items-center gap-1 text-xs text-gray-500 mb-1">
                  {breadcrumb.slice(0, -1).map((part, i) => (
                    <span key={i} className="flex items-center gap-1">
                      {i > 0 && <span className="text-gray-300">/</span>}
                      <span>{part}</span>
                    </span>
                  ))}
                  <span className="text-gray-300">/</span>
                  <span className="font-medium text-gray-700">{breadcrumb[breadcrumb.length - 1]}</span>
                </div>
              )}
              {node.description && (
                <p className="text-sm text-gray-500 line-clamp-1">{node.description}</p>
              )}
              <p className="text-xs text-gray-300 mt-1 font-mono truncate">{node.urn}</p>
            </div>
            {/* Navigate arrow for leaf nodes */}
            {!hasChildren && (
              <ChevronRight className="w-5 h-5 text-gray-300 flex-shrink-0 mt-2" />
            )}
          </div>
        </button>

        {isExpanded && hasChildren && (
          <div className="mt-2 space-y-2">
            {node.children!.map((child) => (
              <TreeNode key={child.urn} node={child} onNavigate={onNavigate} />
            ))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Search Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-8 py-6">
          <div className="flex items-center gap-4 mb-4">
            <button
              onClick={() => navigate('/udf-catalog')}
              className="text-gray-600 hover:text-gray-900"
            >
              ← Back to Catalog
            </button>
          </div>

          <form onSubmit={handleSearch} className="flex gap-3">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search datasets, pipelines, and more..."
                className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cloudera-blue focus:border-transparent"
              />
            </div>
            <button
              type="submit"
              className="px-6 py-3 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700 font-medium"
            >
              Search
            </button>
          </form>

          {/* Filters */}
          <div className="flex items-center gap-3 mt-4">
            <span className="text-sm text-gray-600">Filter by:</span>
            
            {/* Type Filter */}
            <div className="relative">
              <button
                onClick={() => setShowTypeFilter(!showTypeFilter)}
                className={`px-3 py-1.5 border rounded-lg text-sm flex items-center gap-2 ${
                  selectedType ? 'border-cloudera-blue bg-blue-50 text-cloudera-blue' : 'border-gray-300 hover:bg-gray-50'
                }`}
              >
                <Filter className="w-4 h-4" />
                Type {selectedType && `(1)`}
                <ChevronDown className="w-4 h-4" />
              </button>
              
              {showTypeFilter && (
                <div className="absolute top-full left-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-10 min-w-[200px]">
                  {entityTypes.map((type) => {
                    const Icon = type.icon;
                    return (
                      <button
                        key={type.value}
                        onClick={() => {
                          setSelectedType(type.value);
                          setShowTypeFilter(false);
                          const params = new URLSearchParams();
                          if (searchQuery) params.set('q', searchQuery);
                          params.set('type', type.value);
                          if (selectedPlatform) params.set('platform', selectedPlatform);
                          navigate(`/udf-catalog/search?${params.toString()}`);
                        }}
                        className="w-full px-4 py-2 text-left hover:bg-gray-50 flex items-center gap-2"
                      >
                        <Icon className="w-4 h-4 text-gray-600" />
                        <span className="text-sm">{type.label}</span>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Platform Filter */}
            <div className="relative">
              <button
                onClick={() => setShowPlatformFilter(!showPlatformFilter)}
                className={`px-3 py-1.5 border rounded-lg text-sm flex items-center gap-2 ${
                  selectedPlatform ? 'border-cloudera-blue bg-blue-50 text-cloudera-blue' : 'border-gray-300 hover:bg-gray-50'
                }`}
              >
                Platform {selectedPlatform && `(1)`}
                <ChevronDown className="w-4 h-4" />
              </button>
              
              {showPlatformFilter && (
                <div className="absolute top-full left-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-10 min-w-[150px]">
                  {platforms.map((platform) => (
                    <button
                      key={platform}
                      onClick={() => {
                        setSelectedPlatform(platform);
                        setShowPlatformFilter(false);
                        const params = new URLSearchParams();
                        if (searchQuery) params.set('q', searchQuery);
                        if (selectedType) params.set('type', selectedType);
                        params.set('platform', platform);
                        navigate(`/udf-catalog/search?${params.toString()}`);
                      }}
                      className="w-full px-4 py-2 text-left hover:bg-gray-50 text-sm"
                    >
                      {platform}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {hasActiveFilters && (
              <button
                onClick={clearFilters}
                className="px-3 py-1.5 text-sm text-gray-600 hover:text-gray-900 flex items-center gap-1"
              >
                <X className="w-4 h-4" />
                Clear filters
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Results */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        <div className="mb-4 flex items-center justify-between">
          <p className="text-sm text-gray-600">
            {loading ? 'Searching...' : `Found ${totalCount} result${totalCount !== 1 ? 's' : ''}`}
          </p>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-8 h-8 text-cloudera-blue animate-spin" />
          </div>
        ) : results.length > 0 ? (
          <div className="space-y-4">
            {results.map((result) => (
              <TreeNode key={result.urn} node={result} onNavigate={handleEntityClick} />
            ))}
          </div>
        ) : (
          <div className="text-center py-12">
            <Database className="w-16 h-16 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-500 text-lg">No results found</p>
            <p className="text-sm text-gray-400 mt-2">
              Try adjusting your search or filters
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default UDFCatalogSearchV2;
