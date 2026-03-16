import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Search, Workflow, GitBranch, Table, Database, Package, ArrowLeft } from 'lucide-react';

interface Entity {
  urn: string;
  entity_type: string;
  platform: string;
  name: string;
  qualified_name: string;
  updated_at: string;
}

const CatalogSearch = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const query = searchParams.get('q') || '';
  const entityType = searchParams.get('entity_type') || '';
  
  const [results, setResults] = useState<Entity[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (query || entityType) {
      performSearch();
    }
  }, [query, entityType]);

  const performSearch = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (query) params.append('q', query);
      if (entityType) params.append('entity_type', entityType);
      params.append('limit', '50');

      const response = await fetch(`http://localhost:3001/api/catalog/search?${params.toString()}`);
      const data = await response.json();
      setResults(data.entities || []);
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const getEntityIcon = (type: string) => {
    switch (type) {
      case 'pipeline':
        return <Workflow className="w-5 h-5 text-blue-600" />;
      case 'job':
        return <GitBranch className="w-5 h-5 text-green-600" />;
      case 'dataset':
        return <Table className="w-5 h-5 text-purple-600" />;
      case 'datasource':
        return <Database className="w-5 h-5 text-orange-600" />;
      default:
        return <Package className="w-5 h-5 text-gray-600" />;
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

  return (
    <div className="h-full bg-gray-50 overflow-auto">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 p-6">
        <button
          onClick={() => navigate('/catalog')}
          className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-4"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Catalog
        </button>

        <h1 className="text-2xl font-semibold mb-2">
          Search Results
          {entityType && ` - ${entityType.charAt(0).toUpperCase() + entityType.slice(1)}s`}
        </h1>
        
        {query && (
          <p className="text-gray-600">
            Showing results for "<span className="font-medium">{query}</span>"
          </p>
        )}
      </div>

      {/* Results */}
      <div className="p-6">
        {loading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
            <p className="text-gray-600 mt-4">Searching...</p>
          </div>
        ) : results.length === 0 ? (
          <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
            <Search className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No results found</h3>
            <p className="text-gray-600">
              Try different keywords or browse by type
            </p>
          </div>
        ) : (
          <div>
            <p className="text-sm text-gray-600 mb-4">
              Found {results.length} result{results.length !== 1 ? 's' : ''}
            </p>
            
            <div className="space-y-3">
              {results.map((entity) => (
                <button
                  key={entity.urn}
                  onClick={() => navigate(`/catalog/entity/${encodeURIComponent(entity.urn)}`)}
                  className="w-full bg-white border border-gray-200 rounded-lg p-4 hover:border-blue-500 hover:shadow-md transition-all text-left"
                >
                  <div className="flex items-start gap-4">
                    <div className="flex-shrink-0 mt-1">
                      {getEntityIcon(entity.entity_type)}
                    </div>
                    
                    <div className="flex-grow min-w-0">
                      <h3 className="font-medium text-gray-900 mb-1">{entity.name}</h3>
                      <p className="text-sm text-gray-500 mb-2">{entity.qualified_name}</p>
                      
                      <div className="flex items-center gap-2">
                        <span className="text-xs px-2 py-1 bg-gray-100 text-gray-700 rounded">
                          {entity.entity_type}
                        </span>
                        <span className={`text-xs px-2 py-1 rounded ${getPlatformColor(entity.platform)}`}>
                          {entity.platform}
                        </span>
                      </div>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default CatalogSearch;
