import { useState, useEffect } from 'react';
import { ExternalLink, RefreshCw, AlertCircle } from 'lucide-react';

const DataCatalog = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [datahubUrl] = useState(`http://${window.location.hostname}:9002`);

  useEffect(() => {
    // Check if DataHub is accessible
    checkDataHubStatus();
  }, []);

  const checkDataHubStatus = async () => {
    try {
      const response = await fetch('http://localhost:8080/health', { 
        method: 'GET',
        mode: 'no-cors' // Allow cross-origin check
      });
      setLoading(false);
      setError(false);
    } catch (err) {
      console.error('DataHub not accessible:', err);
      setLoading(false);
      setError(true);
    }
  };

  const handleRefresh = () => {
    setLoading(true);
    setError(false);
    // Reload iframe
    const iframe = document.getElementById('datahub-iframe') as HTMLIFrameElement;
    if (iframe) {
      iframe.src = iframe.src;
    }
    setTimeout(() => setLoading(false), 2000);
  };

  const handleOpenInNewTab = () => {
    window.open(datahubUrl, '_blank');
  };

  if (error) {
    return (
      <div className="h-full flex items-center justify-center bg-gray-50">
        <div className="text-center max-w-md">
          <AlertCircle className="w-16 h-16 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">DataHub Not Available</h2>
          <p className="text-gray-600 mb-6">
            DataHub is not running or not accessible at {datahubUrl}
          </p>
          <div className="flex gap-3 justify-center">
            <button
              onClick={checkDataHubStatus}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
            >
              <RefreshCw className="w-4 h-4" />
              Retry
            </button>
            <button
              onClick={handleOpenInNewTab}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 flex items-center gap-2"
            >
              <ExternalLink className="w-4 h-4" />
              Open in New Tab
            </button>
          </div>
          <div className="mt-6 p-4 bg-blue-50 rounded-lg text-left">
            <p className="text-sm text-gray-700 font-medium mb-2">To start DataHub:</p>
            <code className="text-xs bg-gray-900 text-green-400 p-2 rounded block">
              docker-compose -f docker-compose.datahub-quickstart.yml up -d
            </code>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col bg-white">
      {/* Header Bar */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-50 border-b border-gray-200">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
            <span className="text-sm font-medium text-gray-700">DataHub Catalog</span>
          </div>
          <span className="text-xs text-gray-500">Powered by LinkedIn DataHub</span>
        </div>
        
        <div className="flex items-center gap-2">
          <button
            onClick={handleRefresh}
            className="p-2 text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
            title="Refresh"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
          <button
            onClick={handleOpenInNewTab}
            className="p-2 text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
            title="Open in New Tab"
          >
            <ExternalLink className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Loading Overlay */}
      {loading && (
        <div className="absolute inset-0 bg-white/80 flex items-center justify-center z-10">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-600">Loading DataHub...</p>
          </div>
        </div>
      )}

      {/* DataHub Iframe */}
      <iframe
        id="datahub-iframe"
        src={datahubUrl}
        className="flex-1 w-full border-0"
        title="DataHub Catalog"
        onLoad={() => setLoading(false)}
        onError={() => {
          setLoading(false);
          setError(true);
        }}
        sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
        allow="clipboard-read; clipboard-write"
      />
    </div>
  );
};

export default DataCatalog;
