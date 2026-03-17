import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Search,
  Database,
  GitBranch,
  Layers,
  FileText,
  TrendingUp,
  Clock,
  Tag,
  ExternalLink,
  ChevronRight,
  ChevronDown,
  Table2,
  Server,
  FolderOpen,
  Columns,
} from 'lucide-react';
import config from '../config';

interface Entity {
  urn: string;
  type: string;
  name: string;
  platform?: string;
  description?: string;
  properties?: Record<string, string>;
}

interface HierarchyNode {
  server: string;
  databases: {
    name: string;
    schemas: {
      name: string;
      tables: Entity[];
    }[];
  }[];
}

interface PlatformHierarchy {
  platform: string;
  processorCount?: number;
  tableCount?: number;
  hierarchy?: HierarchyNode[];
  processors?: Entity[];
}


const UDFCatalogHome = () => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [recentEntities, setRecentEntities] = useState<Entity[]>([]);
  const [platformHierarchies, setPlatformHierarchies] = useState<PlatformHierarchy[]>([]);
  const [expandedPlatforms, setExpandedPlatforms] = useState<Set<string>>(new Set());
  const [expandedServers, setExpandedServers] = useState<Set<string>>(new Set());
  const [expandedDbs, setExpandedDbs] = useState<Set<string>>(new Set());
  const [expandedSchemas, setExpandedSchemas] = useState<Set<string>>(new Set());
  const [, setExpandedTables] = useState<Set<string>>(new Set());
  // Map from tableUrn -> column names (loaded lazily on expand)
  const [tableColumns, setTableColumns] = useState<Record<string, string[]>>({});
  const [loadingColumns, setLoadingColumns] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchCatalogData();
  }, []);

  const fetchCatalogData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [searchRes, platformsRes] = await Promise.all([
        fetch(`${config.backendUrl}/api/atlas/search?query=*&count=100`),
        fetch(`${config.backendUrl}/api/atlas/platforms`),
      ]);

      if (!searchRes.ok || !platformsRes.ok) throw new Error('Failed to fetch catalog data');

      const searchData = await searchRes.json();
      const platformsData = await platformsRes.json();

      const allEntities: Entity[] = searchData.results.map((e: any) => ({
        urn: e.urn,
        type: e.type,
        name: e.name || e.urn.split('/').pop(),
        platform: e.platform || 'Unknown',
        description: e.description || '',
        properties: e.properties || {},
      }));

      // Recent: processors + tables only
      const recent = allEntities
        .filter(e => e.type === 'TABLE' || (!['CONTAINER', 'COLUMN', 'SCHEMA', 'DATABASE'].includes(e.type)))
        .slice(0, 10);
      setRecentEntities(recent);

      // Build per-platform hierarchies
      const platformStats: Record<string, number> = {};
      platformsData.platforms.forEach((p: any) => { platformStats[p.platform] = p.count; });

      const hierarchies: PlatformHierarchy[] = [];

      // NiFi: list processors
      const nifiProcessors = allEntities.filter(
        e => e.platform === 'NiFi' && !['CONTAINER', 'COLUMN', 'SCHEMA', 'DATABASE'].includes(e.type)
      );
      if (nifiProcessors.length > 0 || platformStats['NiFi']) {
        hierarchies.push({
          platform: 'NiFi',
          processorCount: platformStats['NiFi'] ?? nifiProcessors.length,
          processors: nifiProcessors,
        });
      }

      // JDBC platforms: build Server→DB→Schema→Table tree
      const jdbcPlatforms = [...new Set(
        allEntities.filter(e => e.platform !== 'NiFi').map(e => e.platform!)
      )];

      for (const platform of jdbcPlatforms) {
        const tables = allEntities.filter(e => e.platform === platform && e.type === 'TABLE');
        const tree: Record<string, Record<string, Record<string, Entity[]>>> = {};

        for (const table of tables) {
          const server = table.properties?.server || 'unknown';
          const db = table.properties?.database || 'unknown';
          const schema = table.properties?.schema || 'unknown';
          if (!tree[server]) tree[server] = {};
          if (!tree[server][db]) tree[server][db] = {};
          if (!tree[server][db][schema]) tree[server][db][schema] = [];
          tree[server][db][schema].push(table);
        }

        const hierarchy: HierarchyNode[] = Object.entries(tree).map(([server, dbs]) => ({
          server,
          databases: Object.entries(dbs).map(([dbName, schemas]) => ({
            name: dbName,
            schemas: Object.entries(schemas).map(([schemaName, tbls]) => ({
              name: schemaName,
              tables: tbls,
            })),
          })),
        }));

        hierarchies.push({
          platform,
          tableCount: platformStats[platform] ?? tables.length,
          hierarchy,
        });
      }

      setPlatformHierarchies(hierarchies);

      // Auto-expand all JDBC platforms, servers, dbs, schemas, and tables
      const autoExpandPlatforms = new Set<string>();
      const autoExpandServers = new Set<string>();
      const autoExpandDbs = new Set<string>();
      const autoExpandSchemas = new Set<string>();
      const autoExpandTables = new Set<string>();

      hierarchies.forEach(ph => {
        if (ph.platform !== 'NiFi') {
          autoExpandPlatforms.add(ph.platform);
          ph.hierarchy?.forEach(serverNode => {
            const serverKey = `${ph.platform}::${serverNode.server}`;
            autoExpandServers.add(serverKey);
            serverNode.databases.forEach(dbNode => {
              const dbKey = `${serverKey}::${dbNode.name}`;
              autoExpandDbs.add(dbKey);
              dbNode.schemas.forEach(schemaNode => {
                const schemaKey = `${dbKey}::${schemaNode.name}`;
                autoExpandSchemas.add(schemaKey);
                schemaNode.tables.forEach(table => {
                  autoExpandTables.add(table.urn);
                });
              });
            });
          });
        }
      });

      setExpandedPlatforms(autoExpandPlatforms);
      setExpandedServers(autoExpandServers);
      setExpandedDbs(autoExpandDbs);
      setExpandedSchemas(autoExpandSchemas);
      setExpandedTables(autoExpandTables);

      // Pre-fetch columns for all tables
      const allTableUrns = [...autoExpandTables];
      const colResults = await Promise.all(
        allTableUrns.map(async urn => {
          try {
            const res = await fetch(`${config.backendUrl}/api/atlas/entity/columns?table_urn=${encodeURIComponent(urn)}`);
            if (!res.ok) return { urn, cols: [] as string[] };
            const data = await res.json();
            const cols: string[] = (data.columns || []).map((c: any) =>
              c.name || c.Name || c.urn?.split('/column/').pop() || ''
            ).filter(Boolean);
            return { urn, cols };
          } catch {
            return { urn, cols: [] as string[] };
          }
        })
      );
      const colMap: Record<string, string[]> = {};
      colResults.forEach(({ urn, cols }) => { colMap[urn] = cols; });
      setTableColumns(colMap);

    } catch (err: any) {
      console.error('Failed to fetch catalog data:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const fetchColumnsForTable = async (tableUrn: string) => {
    if (tableColumns[tableUrn] !== undefined || loadingColumns.has(tableUrn)) return;
    setLoadingColumns(prev => new Set(prev).add(tableUrn));
    try {
      const res = await fetch(
        `${config.backendUrl}/api/atlas/entity/columns?table_urn=${encodeURIComponent(tableUrn)}`
      );
      if (res.ok) {
        const data = await res.json();
        // Response is array of column objects or URNs
        const cols: string[] = (data.columns || data || []).map((c: any) => {
          if (typeof c === 'string') return c.split('/column/').pop() || c;
          // ColumnInfoDto has Name (C# Pascal case serialized as camelCase)
          return c.name || c.Name || c.urn?.split('/column/').pop() || c.Urn?.split('/column/').pop() || '';
        }).filter(Boolean);
        setTableColumns(prev => ({ ...prev, [tableUrn]: cols }));
      } else {
        setTableColumns(prev => ({ ...prev, [tableUrn]: [] }));
      }
    } catch {
      setTableColumns(prev => ({ ...prev, [tableUrn]: [] }));
    } finally {
      setLoadingColumns(prev => { const s = new Set(prev); s.delete(tableUrn); return s; });
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/udf-catalog/search?q=${encodeURIComponent(searchQuery)}`);
    }
  };

  const togglePlatform = (platform: string) => {
    setExpandedPlatforms(prev => {
      const next = new Set(prev);
      next.has(platform) ? next.delete(platform) : next.add(platform);
      return next;
    });
  };

  const toggleKey = (setter: React.Dispatch<React.SetStateAction<Set<string>>>, key: string) => {
    setter(prev => {
      const next = new Set(prev);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  };

  const getPlatformColor = (platform: string) => {
    if (platform === 'NiFi') return { bg: 'bg-blue-50', border: 'border-blue-200', badge: 'bg-blue-100 text-blue-700', icon: 'text-blue-600' };
    if (platform === 'MSSQL') return { bg: 'bg-red-50', border: 'border-red-200', badge: 'bg-red-100 text-red-700', icon: 'text-red-600' };
    if (platform === 'Snowflake') return { bg: 'bg-cyan-50', border: 'border-cyan-200', badge: 'bg-cyan-100 text-cyan-700', icon: 'text-cyan-600' };
    return { bg: 'bg-gray-50', border: 'border-gray-200', badge: 'bg-gray-100 text-gray-700', icon: 'text-gray-600' };
  };

  const getEntityTypeIcon = (type: string) => {
    if (type === 'TABLE') return Table2;
    if (type === 'DATABASE') return Database;
    if (type === 'SCHEMA') return FolderOpen;
    if (type === 'CONTAINER') return Layers;
    if (type === 'COLUMN') return Columns;
    if (type?.includes('nifi')) return GitBranch;
    return FileText;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 text-white">
        <div className="max-w-7xl mx-auto px-8 py-12">
          <h1 className="text-4xl font-bold mb-4">UDF Data Catalog</h1>
          <p className="text-blue-100 text-lg mb-8">
            Discover, explore, and understand your data assets with column-level lineage and real-time metadata
          </p>
          <form onSubmit={handleSearch} className="max-w-3xl">
            <div className="relative">
              <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search datasets, pipelines, and more..."
                className="w-full pl-12 pr-4 py-4 text-gray-900 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-300 text-lg"
              />
              <button
                type="submit"
                className="absolute right-2 top-1/2 transform -translate-y-1/2 px-6 py-2 bg-cloudera-blue text-white rounded-md hover:bg-blue-700 font-medium"
              >
                Search
              </button>
            </div>
          </form>
          <div className="flex gap-4 mt-6">
            <button
              onClick={() => navigate('/udf-catalog/search?type=TABLE')}
              className="px-4 py-2 bg-blue-500 hover:bg-blue-600 rounded-md text-sm font-medium transition-colors"
            >
              Browse Tables
            </button>
            <button
              onClick={() => navigate('/udf-catalog/search?platform=NiFi')}
              className="px-4 py-2 bg-blue-500 hover:bg-blue-600 rounded-md text-sm font-medium transition-colors"
            >
              Browse Pipelines
            </button>
            <button
              onClick={() => window.open('http://localhost:9002', '_blank')}
              className="px-4 py-2 bg-white/10 hover:bg-white/20 rounded-md text-sm font-medium transition-colors flex items-center gap-2"
            >
              Open DataHub UI
              <ExternalLink className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
            <p className="font-medium">Error loading catalog data</p>
            <p className="text-sm mt-1">{error}</p>
            <button onClick={fetchCatalogData} className="mt-2 text-sm underline">Try again</button>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left: Platform Hierarchy Tree */}
          <div className="lg:col-span-1 space-y-4">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Layers className="w-5 h-5 text-cloudera-blue" />
                Platforms
              </h2>

              {loading ? (
                <div className="space-y-3">
                  {[1, 2, 3].map(i => <div key={i} className="animate-pulse h-12 bg-gray-100 rounded" />)}
                </div>
              ) : platformHierarchies.length === 0 ? (
                <p className="text-gray-500 text-sm">No platforms found</p>
              ) : (
                <div className="space-y-2">
                  {platformHierarchies.map(ph => {
                    const colors = getPlatformColor(ph.platform);
                    const isExpanded = expandedPlatforms.has(ph.platform);
                    const count = ph.processorCount ?? ph.tableCount ?? 0;

                    return (
                      <div key={ph.platform} className={`rounded-lg border ${colors.border} overflow-hidden`}>
                        {/* Platform header */}
                        <button
                          onClick={() => togglePlatform(ph.platform)}
                          className={`w-full flex items-center justify-between p-3 ${colors.bg} hover:opacity-90 transition-opacity`}
                        >
                          <div className="flex items-center gap-2">
                            {isExpanded
                              ? <ChevronDown className={`w-4 h-4 ${colors.icon}`} />
                              : <ChevronRight className={`w-4 h-4 ${colors.icon}`} />}
                            <Database className={`w-4 h-4 ${colors.icon}`} />
                            <span className="font-semibold text-gray-900">{ph.platform}</span>
                          </div>
                          <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${colors.badge}`}>
                            {count} {ph.platform === 'NiFi' ? 'processors' : 'tables'}
                          </span>
                        </button>

                        {/* Expanded: NiFi processors list */}
                        {isExpanded && ph.platform === 'NiFi' && ph.processors && (
                          <div className="bg-white divide-y divide-gray-100">
                            {ph.processors.map(proc => (
                              <button
                                key={proc.urn}
                                onClick={() => navigate(`/udf-catalog/entity/${encodeURIComponent(proc.urn)}`)}
                                className="w-full flex items-center gap-2 px-4 py-2 hover:bg-blue-50 text-left"
                              >
                                <GitBranch className="w-3.5 h-3.5 text-blue-500 flex-shrink-0" />
                                <span className="text-sm text-gray-800 truncate">{proc.name}</span>
                              </button>
                            ))}
                            <button
                              onClick={() => navigate('/udf-catalog/search?platform=NiFi')}
                              className="w-full text-center py-2 text-xs text-blue-600 hover:bg-blue-50"
                            >
                              View all in search →
                            </button>
                          </div>
                        )}

                        {/* Expanded: JDBC Server→DB→Schema→Table tree */}
                        {isExpanded && ph.platform !== 'NiFi' && ph.hierarchy && (
                          <div className="bg-white">
                            {ph.hierarchy.map(serverNode => {
                              const serverKey = `${ph.platform}::${serverNode.server}`;
                              const serverExpanded = expandedServers.has(serverKey);
                              return (
                                <div key={serverKey}>
                                  {/* Server */}
                                  <button
                                    onClick={() => toggleKey(setExpandedServers, serverKey)}
                                    className="w-full flex items-center gap-2 px-3 py-2 hover:bg-gray-50 border-t border-gray-100"
                                  >
                                    {serverExpanded ? <ChevronDown className="w-3.5 h-3.5 text-gray-400" /> : <ChevronRight className="w-3.5 h-3.5 text-gray-400" />}
                                    <Server className="w-3.5 h-3.5 text-gray-500" />
                                    <span className="text-sm font-medium text-gray-700">{serverNode.server}</span>
                                  </button>

                                  {serverExpanded && serverNode.databases.map(dbNode => {
                                    const dbKey = `${serverKey}::${dbNode.name}`;
                                    const dbExpanded = expandedDbs.has(dbKey);
                                    return (
                                      <div key={dbKey}>
                                        {/* Database */}
                                        <button
                                          onClick={() => toggleKey(setExpandedDbs, dbKey)}
                                          className="w-full flex items-center gap-2 pl-7 pr-3 py-2 hover:bg-gray-50 border-t border-gray-100"
                                        >
                                          {dbExpanded ? <ChevronDown className="w-3.5 h-3.5 text-gray-400" /> : <ChevronRight className="w-3.5 h-3.5 text-gray-400" />}
                                          <Database className="w-3.5 h-3.5 text-indigo-500" />
                                          <span className="text-sm text-gray-700">{dbNode.name}</span>
                                        </button>

                                        {dbExpanded && dbNode.schemas.map(schemaNode => {
                                          const schemaKey = `${dbKey}::${schemaNode.name}`;
                                          const schemaExpanded = expandedSchemas.has(schemaKey);
                                          return (
                                            <div key={schemaKey}>
                                              {/* Schema */}
                                              <button
                                                onClick={() => toggleKey(setExpandedSchemas, schemaKey)}
                                                className="w-full flex items-center gap-2 pl-12 pr-3 py-2 hover:bg-gray-50 border-t border-gray-100"
                                              >
                                                {schemaExpanded ? <ChevronDown className="w-3.5 h-3.5 text-gray-400" /> : <ChevronRight className="w-3.5 h-3.5 text-gray-400" />}
                                                <FolderOpen className="w-3.5 h-3.5 text-yellow-500" />
                                                <span className="text-sm text-gray-700">{schemaNode.name}</span>
                                                <span className="ml-auto text-xs text-gray-400">{schemaNode.tables.length}</span>
                                              </button>

                                              {/* Tables + Columns (always expanded) */}
                                              {schemaExpanded && schemaNode.tables.map(table => {
                                                const cols = tableColumns[table.urn];
                                                const isLoadingCols = loadingColumns.has(table.urn);
                                                return (
                                                  <div key={table.urn}>
                                                    {/* Table row */}
                                                    <div className="flex items-center border-t border-gray-100">
                                                      <button
                                                        onClick={() => navigate(`/udf-catalog/entity/${encodeURIComponent(table.urn)}`)}
                                                        className="flex-1 flex items-center gap-2 pl-16 pr-3 py-2 hover:bg-green-50 text-left"
                                                      >
                                                        <Table2 className="w-3.5 h-3.5 text-green-600 flex-shrink-0" />
                                                        <span className="text-sm text-gray-800 font-medium truncate">{table.name}</span>
                                                        {isLoadingCols && (
                                                          <span className="ml-auto text-xs text-gray-400 animate-pulse">loading...</span>
                                                        )}
                                                        {!isLoadingCols && cols && cols.length > 0 && (
                                                          <span className="ml-auto text-xs text-gray-400">{cols.length} cols</span>
                                                        )}
                                                      </button>
                                                    </div>
                                                    {/* Columns — always shown */}
                                                    {isLoadingCols && (
                                                      <div className="pl-20 pr-3 py-1.5 bg-gray-50 border-t border-gray-100">
                                                        <span className="text-xs text-gray-400 animate-pulse">Loading columns...</span>
                                                      </div>
                                                    )}
                                                    {cols && cols.map(col => {
                                                      const colUrn = `${table.urn}/column/${col}`;
                                                      return (
                                                        <button
                                                          key={col}
                                                          onClick={() => navigate(`/udf-catalog/entity/${encodeURIComponent(colUrn)}`)}
                                                          className="w-full flex items-center gap-2 pl-20 pr-3 py-1 bg-gray-50 border-t border-gray-100 hover:bg-purple-50 text-left transition-colors"
                                                        >
                                                          <Columns className="w-3 h-3 text-purple-400 flex-shrink-0" />
                                                          <span className="text-xs text-gray-600 hover:text-purple-700">{col}</span>
                                                        </button>
                                                      );
                                                    })}
                                                  </div>
                                                );
                                              })}
                                            </div>
                                          );
                                        })}
                                      </div>
                                    );
                                  })}
                                </div>
                              );
                            })}
                            <button
                              onClick={() => navigate(`/udf-catalog/search?platform=${ph.platform}`)}
                              className="w-full text-center py-2 text-xs text-blue-600 hover:bg-blue-50 border-t border-gray-100"
                            >
                              View all in search →
                            </button>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Quick Actions */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4">Quick Actions</h2>
              <div className="space-y-2">
                <button
                  onClick={() => navigate('/udf-catalog/search?type=TABLE')}
                  className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm"
                >
                  <TrendingUp className="w-4 h-4 text-gray-600" />
                  Browse Tables
                </button>
                <button
                  onClick={() => navigate('/udf-catalog/search?platform=NiFi')}
                  className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm"
                >
                  <Clock className="w-4 h-4 text-gray-600" />
                  Browse NiFi Processors
                </button>
                <button
                  onClick={() => navigate('/udf-catalog/search')}
                  className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm"
                >
                  <Tag className="w-4 h-4 text-gray-600" />
                  Search All Entities
                </button>
              </div>
            </div>
          </div>

          {/* Right: Recent Entities */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-lg border border-gray-200">
              <div className="p-6 border-b border-gray-200 flex items-center justify-between">
                <h2 className="text-lg font-semibold flex items-center gap-2">
                  <Clock className="w-5 h-5 text-cloudera-blue" />
                  Recently Updated Entities
                </h2>
                <button onClick={fetchCatalogData} className="p-2 hover:bg-gray-100 rounded-lg" title="Refresh">
                  <Search className="w-4 h-4 text-gray-600" />
                </button>
              </div>

              <div className="divide-y divide-gray-200">
                {loading ? (
                  <div className="p-6 space-y-4">
                    {[1, 2, 3, 4, 5].map(i => (
                      <div key={i} className="animate-pulse h-20 bg-gray-100 rounded" />
                    ))}
                  </div>
                ) : recentEntities.length > 0 ? (
                  recentEntities.map(entity => {
                    const Icon = getEntityTypeIcon(entity.type);
                    const colors = getPlatformColor(entity.platform || '');

                    // Build breadcrumb for TABLE entities
                    const breadcrumb = entity.type === 'TABLE' && entity.properties
                      ? [entity.properties.server, entity.properties.database, entity.properties.schema, entity.name].filter(Boolean)
                      : null;

                    return (
                      <button
                        key={entity.urn}
                        onClick={() => navigate(`/udf-catalog/entity/${encodeURIComponent(entity.urn)}`)}
                        className="w-full p-4 hover:bg-gray-50 transition-colors text-left"
                      >
                        <div className="flex items-start gap-3">
                          <div className={`w-10 h-10 ${colors.bg} rounded-lg flex items-center justify-center flex-shrink-0`}>
                            <Icon className={`w-5 h-5 ${colors.icon}`} />
                          </div>
                          <div className="flex-1 min-w-0">
                            {breadcrumb ? (
                              <div className="flex items-center gap-1 text-xs text-gray-400 mb-1 flex-wrap">
                                {breadcrumb.map((part, i) => (
                                  <span key={i} className="flex items-center gap-1">
                                    {i > 0 && <ChevronRight className="w-3 h-3" />}
                                    <span className={i === breadcrumb.length - 1 ? 'font-semibold text-gray-700' : ''}>{part}</span>
                                  </span>
                                ))}
                              </div>
                            ) : (
                              <h3 className="font-semibold text-gray-900 mb-1 truncate">{entity.name}</h3>
                            )}
                            <div className="flex items-center gap-2">
                              <span className={`inline-block px-2 py-0.5 text-xs font-medium rounded ${colors.badge}`}>
                                {entity.platform}
                              </span>
                              <span className="inline-block px-2 py-0.5 bg-gray-100 text-gray-600 text-xs font-medium rounded">
                                {entity.type === 'TABLE' ? 'TABLE' : entity.type?.split('.').pop()}
                              </span>
                            </div>
                          </div>
                        </div>
                      </button>
                    );
                  })
                ) : (
                  <div className="p-12 text-center">
                    <Database className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                    <p className="text-gray-500">No entities found</p>
                    <p className="text-sm text-gray-400 mt-1">Start by creating NiFi flows or ingesting data</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UDFCatalogHome;
