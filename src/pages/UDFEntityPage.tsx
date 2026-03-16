import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Database,
  GitBranch,
  FileText,
  ExternalLink,
  Tag,
  User,
  Clock,
  Edit,
  Loader2,
  Table,
  GitGraph,
  Info,
  Code,
} from 'lucide-react';
import { LineageViewer } from '../components/LineageViewer';
import config from '../config';

interface EntityData {
  guid?: string;  // Atlas GUID for linking to Atlas UI
  urn: string;
  type: string;
  name: string;
  platform?: string;
  description?: string;
  properties?: Record<string, any>;
  schema?: {
    fields: Array<{
      fieldPath: string;
      type: string;
      nativeDataType: string;
      description?: string;
    }>;
  };
  owners?: Array<{
    owner: {
      username?: string;
      name?: string;
    };
  }>;
  tags?: Array<{
    tag: {
      name: string;
    };
  }>;
  institutionalMemory?: any;
  lastModified?: number;
}

interface ColumnInfo {
  urn: string;
  name: string;
  dataType: string;
  nativeType: string;
  description: string;
}

const UDFEntityPage = () => {
  const { urn } = useParams<{ urn: string }>();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'overview' | 'schema' | 'columns' | 'lineage' | 'properties'>('overview');
  const [entity, setEntity] = useState<EntityData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [childEntities, setChildEntities] = useState<any[]>([]);
  const [tableColumns, setTableColumns] = useState<ColumnInfo[]>([]);

  const DATAHUB_GRAPHQL = 'http://localhost:8080/api/graphql';

  useEffect(() => {
    if (urn) {
      fetchEntityData(decodeURIComponent(urn));
      fetchChildEntities(decodeURIComponent(urn));
    }
  }, [urn]);

  const fetchEntityData = async (entityUrn: string) => {
    try {
      setLoading(true);
      setError(null);

      // Use Atlas API
      console.log('[UDFEntityPage] Using Atlas API - fetching entity:', entityUrn);
      const response = await fetch(`${config.backendUrl}/api/atlas/entity/by-qualified-name?qualified_name=${encodeURIComponent(entityUrn)}`);

      if (!response.ok) {
        throw new Error('Failed to fetch entity');
      }

      const data = await response.json();

      setEntity({
        guid: data.guid,  // Store Atlas GUID
        urn: data.urn,
        type: data.type,
        name: data.name || extractNameFromUrn(data.urn),
        platform: data.platform,
        description: data.description,
        properties: data.properties || {},
        schema: data.schema,
        owners: data.owners?.map((owner: any) => ({
          owner: {
            username: owner.username,
            info: {
              displayName: owner.displayName || owner.username,
            },
          },
        })),
        tags: data.tags?.map((tag: string) => ({
          tag: { name: tag },
        })),
      });

      if (data.type === 'TABLE') {
        fetchTableColumns(data.urn);
      }
    } catch (err: any) {
      console.error('Failed to fetch entity:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const fetchTableColumns = async (tableUrn: string) => {
    try {
      const response = await fetch(`${config.backendUrl}/api/atlas/entity/columns?table_urn=${encodeURIComponent(tableUrn)}`);
      if (response.ok) {
        const data = await response.json();
        setTableColumns(data.columns || []);
      }
    } catch (err) {
      console.error('Failed to fetch table columns:', err);
    }
  };

  const fetchChildEntities = async (parentUrn: string) => {
    try {
      // Use Atlas API for container children
      console.log('[UDFEntityPage] Using Atlas API - fetching children for:', parentUrn);
      const response = await fetch(`${config.backendUrl}/api/atlas/container/${encodeURIComponent(parentUrn)}/children`);
      if (response.ok) {
        const data = await response.json();
        setChildEntities(data.children || []);
        console.log(`[UDFEntityPage] Found ${data.children?.length || 0} child entities for ${parentUrn}`);
      }
    } catch (err) {
      console.error('Failed to fetch child entities:', err);
    }
  };

  const extractNameFromUrn = (urn: string): string => {
    const parts = urn.split(':');
    return parts[parts.length - 1] || urn;
  };

  const getEntityIcon = (type: string) => {
    const typeLower = type?.toLowerCase() || '';
    if (typeLower.includes('flow') || typeLower.includes('job')) return GitBranch;
    if (typeLower.includes('file')) return FileText;
    return Database;
  };

  const openInNiFi = () => {
    if (entity?.properties?.nifiContainerId && entity?.properties?.processGroupId) {
      // Open NiFi UI for this processor - use current hostname for external access
      const hostname = window.location.hostname;
      window.open(`http://${hostname}:9090/nifi/?processGroupId=${entity.properties.processGroupId}`, '_blank');
    }
  };
  
  // Only show "View in NiFi" for actual NiFi processors (not file assets)
  const canViewInNiFi = entity?.platform === 'nifi' && 
                        entity?.properties?.nifiContainerId && 
                        entity?.properties?.processGroupId;

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <Loader2 className="w-8 h-8 text-cloudera-blue animate-spin" />
      </div>
    );
  }

  if (error || !entity) {
    return (
      <div className="min-h-screen bg-gray-50 p-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white rounded-lg border border-red-200 p-8 text-center">
            <p className="text-red-600 text-lg font-medium">Error loading entity</p>
            <p className="text-gray-600 mt-2">{error || 'Entity not found'}</p>
            <button
              onClick={() => navigate('/udf-catalog')}
              className="mt-4 px-4 py-2 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700"
            >
              Back to Catalog
            </button>
          </div>
        </div>
      </div>
    );
  }

  const Icon = getEntityIcon(entity.type);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-8 py-6">
          <button
            onClick={() => navigate('/udf-catalog')}
            className="text-sm text-gray-600 hover:text-gray-900 mb-4"
          >
            ← Back to Catalog
          </button>

          <div className="flex items-start gap-6">
            <div className="w-16 h-16 bg-blue-50 rounded-lg flex items-center justify-center flex-shrink-0">
              <Icon className="w-8 h-8 text-cloudera-blue" />
            </div>

            <div className="flex-1">
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-3xl font-bold text-gray-900">{entity.name}</h1>
                <span className="px-3 py-1 bg-blue-100 text-blue-700 text-sm font-medium rounded">
                  {entity.type}
                </span>
                {entity.platform && (
                  <span className={`px-3 py-1 text-sm font-medium rounded ${
                    entity.platform?.toUpperCase() === 'MSSQL' ? 'bg-red-100 text-red-700' :
                    entity.platform?.toUpperCase() === 'SNOWFLAKE' ? 'bg-cyan-100 text-cyan-700' :
                    entity.platform?.toUpperCase() === 'POSTGRESQL' || entity.platform?.toUpperCase() === 'POSTGRES' ? 'bg-blue-100 text-blue-700' :
                    entity.platform?.toUpperCase() === 'MYSQL' ? 'bg-orange-100 text-orange-700' :
                    entity.platform?.toLowerCase() === 'nifi' ? 'bg-green-100 text-green-700' :
                    'bg-gray-100 text-gray-700'
                  }`}>
                    {entity.platform}
                  </span>
                )}
              </div>

              {/* Hierarchy breadcrumb for TABLE entities */}
              {entity.type === 'TABLE' && entity.properties && (
                <div className="flex items-center gap-1 text-sm text-gray-500 mb-3">
                  {[
                    entity.properties.server,
                    entity.properties.database,
                    entity.properties.schema,
                    entity.name,
                  ].filter(Boolean).map((part, i, arr) => (
                    <span key={i} className="flex items-center gap-1">
                      {i > 0 && <span className="text-gray-300 mx-1">/</span>}
                      <span className={i === arr.length - 1 ? 'font-semibold text-gray-800' : ''}>{part}</span>
                    </span>
                  ))}
                </div>
              )}

              {entity.description && (
                <p className="text-gray-600 mb-4">{entity.description}</p>
              )}

              <div className="flex items-center gap-4 text-sm">
                {entity.owners && entity.owners.length > 0 && (
                  <div className="flex items-center gap-2 text-gray-600">
                    <User className="w-4 h-4" />
                    <span>
                      {entity.owners.map(o => o.owner.username || o.owner.name).join(', ')}
                    </span>
                  </div>
                )}
                {entity.tags && entity.tags.length > 0 && (
                  <div className="flex items-center gap-2 text-gray-600">
                    <Tag className="w-4 h-4" />
                    <span>{entity.tags.length} tag{entity.tags.length !== 1 ? 's' : ''}</span>
                  </div>
                )}
              </div>
            </div>

            <div className="flex gap-2">
              {canViewInNiFi && (
                <button
                  onClick={openInNiFi}
                  className="px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2 text-sm font-medium"
                >
                  View in NiFi
                  <ExternalLink className="w-4 h-4" />
                </button>
              )}
              {entity.guid && (
                <a
                  href={`http://localhost:21000/index.html?action=timeout#!/detailPage/${entity.guid}?tabActive=properties`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2 text-sm font-medium"
                >
                  View in Atlas
                  <ExternalLink className="w-4 h-4" />
                </a>
              )}
              <button className="px-4 py-2 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700 flex items-center gap-2 text-sm font-medium">
                <Edit className="w-4 h-4" />
                Edit
              </button>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex gap-6 mt-6 border-b border-gray-200">
            <button
              onClick={() => setActiveTab('overview')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'overview'
                  ? 'border-cloudera-blue text-cloudera-blue'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              <Info className="w-4 h-4 inline mr-2" />
              Overview
            </button>
            {entity.schema && (
              <button
                onClick={() => setActiveTab('schema')}
                className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                  activeTab === 'schema'
                    ? 'border-cloudera-blue text-cloudera-blue'
                    : 'border-transparent text-gray-600 hover:text-gray-900'
                }`}
              >
                <Table className="w-4 h-4 inline mr-2" />
                Schema ({entity.schema.fields.length})
              </button>
            )}
            {entity.type === 'TABLE' && (
              <button
                onClick={() => setActiveTab('columns')}
                className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                  activeTab === 'columns'
                    ? 'border-cloudera-blue text-cloudera-blue'
                    : 'border-transparent text-gray-600 hover:text-gray-900'
                }`}
              >
                <Table className="w-4 h-4 inline mr-2" />
                Columns {tableColumns.length > 0 ? `(${tableColumns.length})` : ''}
              </button>
            )}
            <button
              onClick={() => setActiveTab('lineage')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'lineage'
                  ? 'border-cloudera-blue text-cloudera-blue'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              <GitGraph className="w-4 h-4 inline mr-2" />
              Lineage
            </button>
            <button
              onClick={() => setActiveTab('properties')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'properties'
                  ? 'border-cloudera-blue text-cloudera-blue'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              <Code className="w-4 h-4 inline mr-2" />
              Properties
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        {activeTab === 'overview' && (
          <div className="space-y-6">
            {/* Description */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4">Description</h2>
              <p className="text-gray-700">
                {entity.description || 'No description available'}
              </p>
            </div>

            {/* Database / Table Info for TABLE and COLUMN entities */}
            {(entity.type === 'TABLE' || entity.type === 'COLUMN') && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <Database className="w-5 h-5 text-gray-600" />
                  Database Information
                </h2>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  {[
                    { label: 'Provider', value: entity.platform },
                    { label: 'Server', value: entity.properties?.server },
                    { label: 'Database', value: entity.properties?.database },
                    { label: 'Schema', value: entity.properties?.schema },
                    ...(entity.type === 'TABLE' ? [{ label: 'Table', value: entity.name }] : []),
                    ...(entity.type === 'COLUMN' ? [
                      { label: 'Table', value: entity.properties?.tableName || entity.properties?.parentFqn?.split('/').pop() },
                      { label: 'Data Type', value: entity.properties?.dataType },
                    ] : []),
                  ].filter(item => item.value).map((item, idx) => (
                    <div key={idx} className="bg-gray-50 rounded-lg p-3">
                      <div className="text-xs text-gray-500 uppercase tracking-wider mb-1">{item.label}</div>
                      <div className="font-medium text-gray-900 text-sm">{item.value}</div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Metadata */}
            <div className="grid grid-cols-2 gap-6">
              {/* Owners */}
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <User className="w-5 h-5 text-gray-600" />
                  Owners
                </h2>
                {entity.owners && entity.owners.length > 0 ? (
                  <div className="space-y-2">
                    {entity.owners.map((owner, idx) => (
                      <div key={idx} className="flex items-center gap-2">
                        <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                          <User className="w-4 h-4 text-cloudera-blue" />
                        </div>
                        <span className="text-sm">{owner.owner.username || owner.owner.name || 'Unknown'}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-sm">No owners assigned</p>
                )}
              </div>

              {/* Tags */}
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <Tag className="w-5 h-5 text-gray-600" />
                  Tags
                </h2>
                {entity.tags && entity.tags.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {entity.tags.map((tag, idx) => (
                      <span
                        key={idx}
                        className="px-3 py-1 bg-blue-50 text-blue-700 text-sm rounded-full"
                      >
                        {tag.tag.name}
                      </span>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-sm">No tags</p>
                )}
              </div>
            </div>

            {/* Child Entities (for containers) */}
            {childEntities.length > 0 && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <Database className="w-5 h-5 text-gray-600" />
                  Contains ({childEntities.length} {childEntities.length === 1 ? 'item' : 'items'})
                </h2>
                <div className="space-y-3">
                  {childEntities.map((child) => (
                    <div
                      key={child.urn}
                      onClick={() => navigate(`/udf-catalog/entity/${encodeURIComponent(child.urn)}`)}
                      className="flex items-center gap-4 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
                    >
                      <div className="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center flex-shrink-0">
                        {child.type === 'DATA_JOB' ? (
                          <GitBranch className="w-5 h-5 text-cloudera-blue" />
                        ) : (
                          <Database className="w-5 h-5 text-cloudera-blue" />
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="font-medium text-gray-900 truncate">{child.name}</h3>
                        <div className="flex items-center gap-2 mt-1">
                          <span className="px-2 py-0.5 bg-gray-100 text-gray-600 text-xs rounded">
                            {child.type}
                          </span>
                          {child.properties?.processor_type && (
                            <span className="text-xs text-gray-500">
                              {child.properties.processor_type}
                            </span>
                          )}
                        </div>
                      </div>
                      <ExternalLink className="w-4 h-4 text-gray-400" />
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* URN */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold mb-4">Entity URN</h2>
              <code className="block p-3 bg-gray-50 rounded text-sm font-mono break-all">
                {entity.urn}
              </code>
            </div>
          </div>
        )}

        {activeTab === 'schema' && entity.schema && (
          <div className="bg-white rounded-lg border border-gray-200">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-200">
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Column Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Native Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Description
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {entity.schema.fields.map((field, idx) => (
                    <tr key={idx} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">
                        {field.fieldPath}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-700">
                        <code className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs">
                          {field.type}
                        </code>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {field.nativeDataType}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {field.description || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'columns' && entity.type === 'TABLE' && (
          <div className="bg-white rounded-lg border border-gray-200">
            {tableColumns.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <Table className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                <p>No columns found for this table.</p>
                <p className="text-xs mt-1 text-gray-400">Columns are populated during metadata ingestion.</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="bg-gray-50 border-b border-gray-200">
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">Column Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">Data Type</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">Description</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {tableColumns.map((col, idx) => (
                      <tr key={idx} className="hover:bg-gray-50">
                        <td className="px-6 py-4 text-sm font-medium text-gray-900">{col.name}</td>
                        <td className="px-6 py-4 text-sm text-gray-700">
                          <code className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs">
                            {col.dataType || col.nativeType || 'VARCHAR'}
                          </code>
                        </td>
                        <td className="px-6 py-4 text-sm text-gray-600">{col.description || '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {activeTab === 'lineage' && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <LineageViewer assetUrn={entity.urn} assetName={entity.name} />
          </div>
        )}

        {activeTab === 'properties' && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4">Custom Properties</h2>
            {entity.properties && Object.keys(entity.properties).length > 0 ? (
              <div className="space-y-3">
                {Object.entries(entity.properties)
                  .filter(([key, value]) => {
                    // Skip these known complex object keys
                    const skipKeys = [
                      'columns', 'processGroup', 'parentProcessGroup', 'parentContainer',
                      'childProcessGroups', 'processors', 'inputToProcesses', 'outputFromProcesses',
                      'table', 'processor', 'inputColumns', 'outputColumns', 
                      'upstreamDatabaseColumns', 'downstreamDatabaseColumns'
                    ];
                    if (skipKeys.includes(key)) {
                      return false;
                    }
                    
                    // Skip arrays and objects (except nested 'properties' object)
                    if (key !== 'properties' && typeof value === 'object' && value !== null) {
                      return false;
                    }
                    
                    return true;
                  })
                  .map(([key, value]) => {
                    // Handle nested properties object
                    if (key === 'properties' && typeof value === 'object' && value !== null) {
                      return Object.entries(value as Record<string, any>)
                        .filter(([propKey, propValue]) => {
                          // Skip complex objects in nested properties too
                          if (typeof propValue === 'object' && propValue !== null) {
                            return false;
                          }
                          return true;
                        })
                        .map(([propKey, propValue]) => {
                          const isSQL = propKey.toLowerCase().includes('sql') || propKey.toLowerCase().includes('query');
                          return (
                            <div key={`${key}.${propKey}`} className="border-b border-gray-100 pb-3">
                              <div className="text-sm font-medium text-gray-600 mb-1">{propKey}</div>
                              {isSQL && propValue ? (
                                <pre className="text-sm text-gray-900 bg-gray-50 p-3 rounded border border-gray-200 overflow-x-auto font-mono">
                                  {String(propValue)}
                                </pre>
                              ) : (
                                <div className="text-sm text-gray-900">{propValue ? String(propValue) : 'null'}</div>
                              )}
                            </div>
                          );
                        });
                    }
                    
                    // Render simple properties
                    const isSQL = key.toLowerCase().includes('sql') || key.toLowerCase().includes('query');
                    return (
                      <div key={key} className="border-b border-gray-100 pb-3">
                        <div className="text-sm font-medium text-gray-600 mb-1">{key}</div>
                        {isSQL && value ? (
                          <pre className="text-sm text-gray-900 bg-gray-50 p-3 rounded border border-gray-200 overflow-x-auto font-mono">
                            {String(value)}
                          </pre>
                        ) : (
                          <div className="text-sm text-gray-900">{value ? String(value) : 'null'}</div>
                        )}
                      </div>
                    );
                  })}
              </div>
            ) : (
              <p className="text-gray-500 text-sm">No custom properties</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default UDFEntityPage;
