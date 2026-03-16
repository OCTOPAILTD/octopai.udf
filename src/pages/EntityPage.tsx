import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { ExternalLink, GitBranch, Tag, User, Clock, ArrowLeft, Workflow, Package, Info } from 'lucide-react';

interface Entity {
  urn: string;
  entity_type: string;
  platform: string;
  name: string;
  qualified_name: string;
  created_at: string;
  updated_at: string;
}

interface EntityData {
  entity: Entity;
  aspects: {
    properties?: {
      description?: string;
      [key: string]: any;
    };
    ownership?: {
      owners?: string[];
    };
    tags?: {
      tags?: string[];
    };
    links?: {
      source_url?: string;
    };
    schema?: {
      fields?: Array<{
        field_path: string;
        field_type?: string;
        description?: string;
      }>;
    };
  };
}

const EntityPage = () => {
  const { urn } = useParams<{ urn: string }>();
  const navigate = useNavigate();
  const [entity, setEntity] = useState<EntityData | null>(null);
  const [activeTab, setActiveTab] = useState('overview');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (urn) {
      loadEntity();
    }
  }, [urn]);

  const loadEntity = async () => {
    try {
      const response = await fetch(`http://localhost:3001/api/catalog/entities/${encodeURIComponent(urn!)}`);
      if (response.ok) {
        const data = await response.json();
        setEntity(data);
      } else {
        console.error('Failed to load entity');
      }
    } catch (error) {
      console.error('Failed to load entity:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="h-full bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
          <p className="text-gray-600 mt-4">Loading entity...</p>
        </div>
      </div>
    );
  }

  if (!entity) {
    return (
      <div className="h-full bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <Package className="w-16 h-16 text-gray-400 mx-auto mb-4" />
          <h2 className="text-xl font-medium text-gray-900 mb-2">Entity not found</h2>
          <button
            onClick={() => navigate('/catalog')}
            className="text-blue-600 hover:text-blue-700"
          >
            Go back to catalog
          </button>
        </div>
      </div>
    );
  }

  const { entity: entityInfo, aspects } = entity;

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

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'pipeline':
        return 'bg-blue-100 text-blue-800';
      case 'job':
        return 'bg-green-100 text-green-800';
      case 'dataset':
        return 'bg-purple-100 text-purple-800';
      case 'datasource':
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

        <div className="flex items-start justify-between">
          <div className="flex-grow">
            <div className="flex items-center gap-2 text-sm mb-2">
              <span className={`px-2 py-1 rounded font-medium ${getTypeColor(entityInfo.entity_type)}`}>
                {entityInfo.entity_type}
              </span>
              <span className={`px-2 py-1 rounded font-medium ${getPlatformColor(entityInfo.platform)}`}>
                {entityInfo.platform}
              </span>
            </div>
            
            <h1 className="text-2xl font-semibold mb-1">{entityInfo.name}</h1>
            <p className="text-sm text-gray-600">{entityInfo.qualified_name}</p>
          </div>
          
          {/* Link back to source system */}
          {aspects.links?.source_url && (
            <a
              href={aspects.links.source_url}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              <ExternalLink className="w-4 h-4" />
              Open in {entityInfo.platform.toUpperCase()}
            </a>
          )}
        </div>

        {/* Metadata badges */}
        <div className="mt-4 flex flex-wrap items-center gap-4 text-sm">
          {aspects.ownership?.owners && aspects.ownership.owners.length > 0 && (
            <div className="flex items-center gap-1 text-gray-600">
              <User className="w-4 h-4" />
              <span>{aspects.ownership.owners.join(', ')}</span>
            </div>
          )}
          
          {aspects.tags?.tags && aspects.tags.tags.length > 0 && (
            <div className="flex items-center gap-2">
              <Tag className="w-4 h-4 text-gray-400" />
              {aspects.tags.tags.map((tag) => (
                <span key={tag} className="px-2 py-0.5 bg-gray-100 rounded text-gray-700">
                  {tag}
                </span>
              ))}
            </div>
          )}
          
          <div className="flex items-center gap-1 text-gray-500">
            <Clock className="w-4 h-4" />
            <span>Updated {new Date(entityInfo.updated_at).toLocaleString()}</span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white border-b border-gray-200 px-6">
        <nav className="flex gap-6">
          {['overview', 'schema', 'lineage'].map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`py-3 border-b-2 font-medium text-sm transition-colors ${
                activeTab === tab
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab content */}
      <div className="p-6">
        {activeTab === 'overview' && (
          <div className="space-y-6">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Info className="w-5 h-5" />
                Description
              </h2>
              <p className="text-gray-700">
                {aspects.properties?.description || 'No description available'}
              </p>
            </div>

            {aspects.properties && Object.keys(aspects.properties).length > 1 && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h2 className="text-lg font-semibold mb-4">Properties</h2>
                <dl className="grid grid-cols-2 gap-4">
                  {Object.entries(aspects.properties).map(([key, value]) => {
                    if (key === 'description') return null;
                    return (
                      <div key={key}>
                        <dt className="text-sm font-medium text-gray-500">{key}</dt>
                        <dd className="mt-1 text-sm text-gray-900">
                          {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                        </dd>
                      </div>
                    );
                  })}
                </dl>
              </div>
            )}
          </div>
        )}

        {activeTab === 'schema' && (
          <div className="bg-white rounded-lg border border-gray-200">
            {aspects.schema?.fields && aspects.schema.fields.length > 0 ? (
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Field
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Description
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {aspects.schema.fields.map((field, idx) => (
                    <tr key={idx}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {field.field_path}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {field.field_type || '-'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {field.description || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="p-12 text-center">
                <p className="text-gray-600">No schema information available</p>
              </div>
            )}
          </div>
        )}

        {activeTab === 'lineage' && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="text-center py-8">
              <GitBranch className="w-12 h-12 text-gray-400 mx-auto mb-3" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">Lineage Viewer</h3>
              <p className="text-gray-600 mb-4">
                View upstream and downstream lineage for this entity
              </p>
              <Link
                to={`/catalog/lineage/${encodeURIComponent(urn!)}`}
                className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                <GitBranch className="w-4 h-4" />
                View Lineage Graph
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default EntityPage;
