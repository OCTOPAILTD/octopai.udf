import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Search,
  Database,
  GitBranch,
  Layers,
  FileText,
  Filter,
  TrendingUp,
  Clock,
  Tag,
  ExternalLink,
} from 'lucide-react';
import config from '../config';

interface Entity {
  urn: string;
  type: string;
  name: string;
  platform?: string;
  description?: string;
  lastModified?: number;
}

interface PlatformStats {
  platform: string;
  count: number;
  icon: any;
}

const UDFCatalogHome = () => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [recentEntities, setRecentEntities] = useState<Entity[]>([]);
  const [platformStats, setPlatformStats] = useState<PlatformStats[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const DATAHUB_GRAPHQL = 'http://localhost:8080/api/graphql';

  // Fetch recent entities and platform stats
  useEffect(() => {
    fetchCatalogData();
  }, []);

  const fetchCatalogData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Use Atlas API
      console.log('[UDFCatalogHome] Using Atlas API - fetching catalog data');
      const searchResponse = await fetch(`${config.backendUrl}/api/atlas/search?query=*&count=100`);
      const platformsResponse = await fetch(`${config.backendUrl}/api/atlas/platforms`);

      if (!searchResponse.ok || !platformsResponse.ok) {
        throw new Error('Failed to fetch catalog data');
      }

      const searchData = await searchResponse.json();
      const platformsData = await platformsResponse.json();

      const entities = searchData.results.map((entity: any) => ({
        urn: entity.urn,
        type: entity.type,
        name: entity.name || extractNameFromUrn(entity.urn),
        platform: entity.platform || 'Unknown',
        description: entity.description || '',
      }));

      setRecentEntities(entities.slice(0, 10));

      // Use platform stats from backend
      const platformStatsArray = platformsData.platforms.map((p: any) => ({
        platform: p.platform,
        count: p.count,
        icon: getPlatformIcon(p.platform),
      }));

      setPlatformStats(platformStatsArray);
    } catch (err: any) {
      console.error('Failed to fetch catalog data:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const extractNameFromUrn = (urn: string): string => {
    const parts = urn.split(':');
    return parts[parts.length - 1] || urn;
  };

  const getPlatformIcon = (platform: string) => {
    const platformLower = platform.toLowerCase();
    if (platformLower.includes('nifi')) return GitBranch;
    if (platformLower.includes('file')) return FileText;
    if (platformLower.includes('kafka')) return Layers;
    return Database;
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/udf-catalog/search?q=${encodeURIComponent(searchQuery)}`);
    }
  };

  const handleEntityClick = (urn: string) => {
    navigate(`/udf-catalog/entity/${encodeURIComponent(urn)}`);
  };

  const getEntityTypeIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'dataset':
        return Database;
      case 'dataflow':
      case 'dataprocess':
        return GitBranch;
      default:
        return FileText;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero Section */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 text-white">
        <div className="max-w-7xl mx-auto px-8 py-12">
          <h1 className="text-4xl font-bold mb-4">UDF Data Catalog</h1>
          <p className="text-blue-100 text-lg mb-8">
            Discover, explore, and understand your data assets with column-level lineage and real-time metadata
          </p>

          {/* Search Bar */}
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

          {/* Quick Links */}
          <div className="flex gap-4 mt-6">
            <button
              onClick={() => navigate('/udf-catalog/search?type=dataset')}
              className="px-4 py-2 bg-blue-500 hover:bg-blue-600 rounded-md text-sm font-medium transition-colors"
            >
              Browse Datasets
            </button>
            <button
              onClick={() => navigate('/udf-catalog/search?type=dataFlow')}
              className="px-4 py-2 bg-blue-500 hover:bg-blue-600 rounded-md text-sm font-medium transition-colors"
            >
              Browse Pipelines
            </button>
            <button
              onClick={() => window.open(config.dataHubUrl, '_blank')}
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
            <button
              onClick={fetchCatalogData}
              className="mt-2 text-sm underline hover:no-underline"
            >
              Try again
            </button>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left Column - Platform Stats */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Layers className="w-5 h-5 text-cloudera-blue" />
                Platforms
              </h2>
              
              {loading ? (
                <div className="space-y-3">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="animate-pulse">
                      <div className="h-12 bg-gray-100 rounded"></div>
                    </div>
                  ))}
                </div>
              ) : platformStats.length > 0 ? (
                <div className="space-y-2">
                  {platformStats.map((stat) => {
                    const Icon = stat.icon;
                    return (
                      <button
                        key={stat.platform}
                        onClick={() => navigate(`/udf-catalog/search?platform=${stat.platform}`)}
                        className="w-full flex items-center justify-between p-3 rounded-lg hover:bg-gray-50 transition-colors border border-gray-100"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center">
                            <Icon className="w-5 h-5 text-cloudera-blue" />
                          </div>
                          <span className="font-medium text-gray-900">{stat.platform}</span>
                        </div>
                        <span className="text-2xl font-semibold text-cloudera-blue">{stat.count}</span>
                      </button>
                    );
                  })}
                </div>
              ) : (
                <p className="text-gray-500 text-sm">No platforms found</p>
              )}
            </div>

            {/* Quick Actions */}
            <div className="mt-6 bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4">Quick Actions</h2>
              <div className="space-y-2">
                <button className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm">
                  <TrendingUp className="w-4 h-4 text-gray-600" />
                  Most Popular Datasets
                </button>
                <button className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm">
                  <Clock className="w-4 h-4 text-gray-600" />
                  Recently Updated
                </button>
                <button className="w-full text-left px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2 text-sm">
                  <Tag className="w-4 h-4 text-gray-600" />
                  Browse by Tags
                </button>
              </div>
            </div>
          </div>

          {/* Right Column - Recent Entities */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-lg border border-gray-200">
              <div className="p-6 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h2 className="text-lg font-semibold flex items-center gap-2">
                    <Clock className="w-5 h-5 text-cloudera-blue" />
                    Recently Updated Entities
                  </h2>
                  <button
                    onClick={fetchCatalogData}
                    className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                    title="Refresh"
                  >
                    <Filter className="w-4 h-4 text-gray-600" />
                  </button>
                </div>
              </div>

              <div className="divide-y divide-gray-200">
                {loading ? (
                  <div className="p-6 space-y-4">
                    {[1, 2, 3, 4, 5].map((i) => (
                      <div key={i} className="animate-pulse">
                        <div className="h-20 bg-gray-100 rounded"></div>
                      </div>
                    ))}
                  </div>
                ) : recentEntities.length > 0 ? (
                  recentEntities.map((entity) => {
                    const Icon = getEntityTypeIcon(entity.type);
                    return (
                      <button
                        key={entity.urn}
                        onClick={() => handleEntityClick(entity.urn)}
                        className="w-full p-6 hover:bg-gray-50 transition-colors text-left"
                      >
                        <div className="flex items-start gap-4">
                          <div className="w-12 h-12 bg-blue-50 rounded-lg flex items-center justify-center flex-shrink-0">
                            <Icon className="w-6 h-6 text-cloudera-blue" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <h3 className="font-semibold text-gray-900 mb-1 truncate">
                              {entity.name}
                            </h3>
                            <div className="flex items-center gap-2 mb-2">
                              <span className="inline-block px-2 py-0.5 bg-blue-100 text-blue-700 text-xs font-medium rounded">
                                {entity.type}
                              </span>
                              {entity.platform && (
                                <span className="inline-block px-2 py-0.5 bg-gray-100 text-gray-700 text-xs font-medium rounded">
                                  {entity.platform}
                                </span>
                              )}
                            </div>
                            {entity.description && (
                              <p className="text-sm text-gray-600 line-clamp-2">
                                {entity.description}
                              </p>
                            )}
                          </div>
                        </div>
                      </button>
                    );
                  })
                ) : (
                  <div className="p-12 text-center">
                    <Database className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                    <p className="text-gray-500">No entities found</p>
                    <p className="text-sm text-gray-400 mt-1">
                      Start by creating NiFi flows or ingesting data
                    </p>
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
