import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Database, GitBranch, Clock, Filter, Package, Workflow, Table } from 'lucide-react';

interface Entity {
  urn: string;
  entity_type: string;
  platform: string;
  name: string;
  qualified_name: string;
  updated_at: string;
}

const CatalogHome = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [recentEntities, setRecentEntities] = useState<Entity[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    loadRecentEntities();
  }, []);

  const loadRecentEntities = async () => {
    try {
      const response = await fetch('http://localhost:3001/api/catalog/entities?limit=10&sort=updated_at');
      const data = await response.json();
      setRecentEntities(data.entities || []);
    } catch (error) {
      console.error('Failed to load recent entities:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/catalog/search?q=${encodeURIComponent(searchQuery)}`);
    }
  };

  const getEntityIcon = (entityType: string) => {
    switch (entityType) {
      case 'pipeline':
        return <Workflow className="w-5 h-5" />;
      case 'job':
        return <GitBranch className="w-5 h-5" />;
      case 'dataset':
        return <Table className="w-5 h-5" />;
      case 'datasource':
        return <Database className="w-5 h-5" />;
      default:
        return <Package className="w-5 h-5" />;
    }
  };

  const getPlatformColor = (platform: string) => {
    switch (platform) {
      case 'nifi':
        return 'bg-blue-100 text-blue-800';
      case 'kafka':
        return 'bg-purple-100 text-purple-800';
      case 'hive':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minutes ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)} hours ago`;
    return `${Math.floor(diffMins / 1440)} days ago`;
  };

  return (
    <div className="h-full bg-gray-50 overflow-auto">
      {/* Header with search */}
      <div className="bg-white border-b border-gray-200 p-6">
        <h1 className="text-2xl font-semibold mb-4">Data Catalog</h1>
        
        <form onSubmit={handleSearch} className="max-w-2xl">
          <div className="relative">
            <Search className="absolute left-3 top-3 w-5 h-5 text-gray-400" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search datasets, pipelines, processors..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </form>
      </div>

      {/* Browse by type */}
      <div className="p-6">
        <h2 className="text-lg font-semibold mb-4">Browse by Type</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <button
            onClick={() => navigate('/catalog/search?entity_type=pipeline')}
            className="p-4 bg-white border border-gray-200 rounded-lg hover:border-blue-500 hover:shadow-md transition-all"
          >
            <Workflow className="w-8 h-8 text-blue-600 mb-2" />
            <div className="font-medium">Pipelines</div>
            <div className="text-sm text-gray-500">Data flows</div>
          </button>

          <button
            onClick={() => navigate('/catalog/search?entity_type=job')}
            className="p-4 bg-white border border-gray-200 rounded-lg hover:border-green-500 hover:shadow-md transition-all"
          >
            <GitBranch className="w-8 h-8 text-green-600 mb-2" />
            <div className="font-medium">Jobs</div>
            <div className="text-sm text-gray-500">Processors & tasks</div>
          </button>

          <button
            onClick={() => navigate('/catalog/search?entity_type=dataset')}
            className="p-4 bg-white border border-gray-200 rounded-lg hover:border-purple-500 hover:shadow-md transition-all"
          >
            <Table className="w-8 h-8 text-purple-600 mb-2" />
            <div className="font-medium">Datasets</div>
            <div className="text-sm text-gray-500">Tables & files</div>
          </button>

          <button
            onClick={() => navigate('/catalog/search?entity_type=datasource')}
            className="p-4 bg-white border border-gray-200 rounded-lg hover:border-orange-500 hover:shadow-md transition-all"
          >
            <Database className="w-8 h-8 text-orange-600 mb-2" />
            <div className="font-medium">Data Sources</div>
            <div className="text-sm text-gray-500">Connections</div>
          </button>
        </div>
      </div>

      {/* Recently updated */}
      <div className="p-6">
        <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Clock className="w-5 h-5" />
          Recently Updated
        </h2>
        
        {loading ? (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500 mx-auto"></div>
            <p className="text-gray-600 mt-2">Loading entities...</p>
          </div>
        ) : recentEntities.length === 0 ? (
          <div className="bg-white rounded-lg border border-gray-200 p-8 text-center">
            <Package className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-600">No entities yet</p>
            <p className="text-sm text-gray-500 mt-1">
              Create a NiFi flow to start populating the catalog
            </p>
          </div>
        ) : (
          <div className="bg-white rounded-lg border border-gray-200 divide-y">
            {recentEntities.map((entity) => (
              <button
                key={entity.urn}
                onClick={() => navigate(`/catalog/entity/${encodeURIComponent(entity.urn)}`)}
                className="w-full p-4 hover:bg-gray-50 transition-colors text-left flex items-center gap-4"
              >
                <div className="flex-shrink-0">
                  {getEntityIcon(entity.entity_type)}
                </div>
                
                <div className="flex-grow min-w-0">
                  <div className="font-medium truncate">{entity.name}</div>
                  <div className="text-sm text-gray-500 truncate">{entity.qualified_name}</div>
                </div>
                
                <div className="flex items-center gap-2 flex-shrink-0">
                  <span className={`px-2 py-1 text-xs rounded ${getPlatformColor(entity.platform)}`}>
                    {entity.platform}
                  </span>
                  <span className="text-sm text-gray-500">
                    {formatDate(entity.updated_at)}
                  </span>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default CatalogHome;
