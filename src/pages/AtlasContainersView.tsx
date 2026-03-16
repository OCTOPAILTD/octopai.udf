import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Database, Server, FolderTree, Loader2, ExternalLink } from 'lucide-react';
import config from '../config';

interface AtlasContainer {
  guid: string;
  name: string;
  qualifiedName: string;
  containerId: string;
  processGroupCount: number;
  urn: string;
}

const AtlasContainersView = () => {
  const navigate = useNavigate();
  const [containers, setContainers] = useState<AtlasContainer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadAtlasContainers();
  }, []);

  const loadAtlasContainers = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await fetch(`${config.backendUrl}/api/atlas/hierarchy/containers`);
      
      if (!response.ok) {
        throw new Error(`Failed to load containers: ${response.statusText}`);
      }
      
      const data = await response.json();
      console.log('[AtlasContainers] Loaded:', data);
      
      setContainers(data.containers || []);
    } catch (err) {
      console.error('[AtlasContainers] Error:', err);
      setError(err instanceof Error ? err.message : 'Failed to load containers');
    } finally {
      setLoading(false);
    }
  };

  const handleContainerClick = (container: AtlasContainer) => {
    // Navigate to UDF Catalog search filtered by this container
    navigate(`/udf-catalog/search?platform=NiFi&query=${encodeURIComponent(container.name)}`);
  };

  const handleOpenInAtlas = (container: AtlasContainer) => {
    // Open in Atlas UI
    window.open(`http://localhost:21000/index.html#!/detailPage/${container.guid}`, '_blank');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 text-cloudera-blue animate-spin" />
        <span className="ml-3 text-gray-600">Loading Atlas containers...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-6">
        <h3 className="text-red-800 font-semibold mb-2">Error Loading Containers</h3>
        <p className="text-red-600 text-sm">{error}</p>
        <button
          onClick={loadAtlasContainers}
          className="mt-4 px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  if (containers.length === 0) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-8 text-center">
        <Server className="w-12 h-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-semibold text-gray-700 mb-2">No NiFi Containers Found</h3>
        <p className="text-gray-600 text-sm">
          No NiFi metadata containers are available in Atlas yet.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">
          NiFi Metadata Containers
        </h2>
        <span className="text-sm text-gray-600">
          {containers.length} container{containers.length !== 1 ? 's' : ''}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {containers.map((container) => (
          <div
            key={container.guid}
            className="bg-white border-2 border-gray-200 rounded-lg p-6 hover:border-cloudera-blue hover:shadow-md transition-all cursor-pointer"
            onClick={() => handleContainerClick(container)}
          >
            <div className="flex items-start justify-between mb-4">
              <div className="w-12 h-12 bg-blue-50 rounded-lg flex items-center justify-center">
                <Server className="w-6 h-6 text-cloudera-blue" />
              </div>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  handleOpenInAtlas(container);
                }}
                className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                title="Open in Atlas"
              >
                <ExternalLink className="w-4 h-4 text-gray-600" />
              </button>
            </div>

            <h3 className="font-semibold text-gray-900 mb-2">
              {container.name}
            </h3>

            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <FolderTree className="w-4 h-4" />
                <span>{container.processGroupCount} Process Groups</span>
              </div>

              <div className="text-xs text-gray-500 font-mono truncate" title={container.qualifiedName}>
                {container.qualifiedName}
              </div>
            </div>

            <div className="mt-4 pt-4 border-t border-gray-200">
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  navigate(`/udf-catalog/search?platform=NiFi`);
                }}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                <Database className="w-4 h-4" />
                Browse in UDF Catalog
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default AtlasContainersView;
