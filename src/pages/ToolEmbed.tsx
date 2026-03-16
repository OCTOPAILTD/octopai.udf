import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { X, ExternalLink, RefreshCw, Maximize2, Loader2, Database } from 'lucide-react';
import config from '../config';

// Get the correct API URL based on environment
const getApiUrl = () => {
  // Dev mode (Vite dev server on port 5173)
  if (window.location.port === '5173') {
    return config.backendUrl;
  }
  // Production mode (Nginx proxy) - use empty string for relative URLs
  return '';
};

interface ContainerHealth {
  ready: boolean;
  status: string;
  progress?: number;
  message: string;
}

const ToolEmbed = () => {
  const { tool, containerId } = useParams();
  const navigate = useNavigate();
  const [iframeUrl, setIframeUrl] = useState<string>('');
  const [directUrl, setDirectUrl] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [health, setHealth] = useState<ContainerHealth>({
    ready: false,
    status: 'starting',
    progress: 0,
    message: 'Starting container...'
  });
  const [showIframe, setShowIframe] = useState(false);

  // Check container health
  useEffect(() => {
    if (!containerId) return;

    let checkCount = 0;
    const maxChecks = 60; // Maximum 60 checks (3 minutes at 3 second intervals)

    const checkHealth = async () => {
      try {
        const response = await fetch(`${getApiUrl()}/api/containers/${containerId}/health`);
        const data = await response.json();
        
        checkCount++;
        
        // Calculate progress based on check count and status
        let progress = Math.min(10 + (checkCount * 2), 95);
        let message = 'Checking service health...';
        let statusType = 'starting';
        
        if (data.status === 'running') {
          progress = Math.min(50 + (checkCount * 5), 95);
          
          if (tool === 'nifi') {
            if (data.nifi_ready) {
              progress = 100;
              message = 'NiFi is ready! Opening interface...';
              statusType = 'ready';
            } else {
              message = 'NiFi is starting up... This usually takes 30-60 seconds.';
              statusType = 'initializing';
            }
          } else {
            if (data.ready) {
              progress = 100;
              message = 'Container is ready! Opening interface...';
              statusType = 'ready';
            } else {
              message = 'Container is initializing...';
              statusType = 'initializing';
            }
          }
        } else {
          message = `Container status: ${data.status}`;
        }
        
        // Update health state
        setHealth({
          ready: tool === 'nifi' ? (data.nifi_ready || false) : (data.ready || false),
          status: statusType,
          progress: progress,
          message: message
        });

        // If ready, show the iframe
        if ((tool === 'nifi' && data.nifi_ready) || (tool !== 'nifi' && data.ready)) {
          setTimeout(() => {
            setShowIframe(true);
            setError(''); // Clear any errors
          }, 500);
        }
      } catch (err) {
        console.error('Health check failed:', err);
        setHealth({
          ready: false,
          status: 'starting',
          progress: Math.min(10 + (checkCount * 2), 40),
          message: 'Waiting for service to respond...'
        });
      }
    };

    // Initial check immediately
    checkHealth();

    // Poll every 3 seconds until ready or max checks reached
    const interval = setInterval(() => {
      if (!showIframe && checkCount < maxChecks) {
        checkHealth();
      } else if (checkCount >= maxChecks) {
        clearInterval(interval);
        setError('Container took too long to start. Please check the container logs or try restarting it.');
      }
    }, 3000);

    return () => {
      clearInterval(interval);
    };
  }, [containerId, tool, showIframe]);

  // Get container URL
  useEffect(() => {
    // Clear any cached container data for this container to force fresh fetch
    localStorage.removeItem(`container_${containerId}`);
    
    const loadContainerInfo = async () => {
      // Always fetch fresh data from backend to ensure port is correct
      // (fixes issue where localStorage has old random port instead of fixed 9090)
      console.log('Fetching container info from backend...');
      try {
        // Add timestamp to bust any HTTP cache
        const response = await fetch(`${getApiUrl()}/api/containers?t=${Date.now()}`, {
          cache: 'no-store',
          headers: {
            'Cache-Control': 'no-cache'
          }
        });
        const result = await response.json();
        
        if (result.success) {
          const container = result.containers.find((c: any) => c.id.startsWith(containerId.substring(0, 12)));
          
          if (container && container.ports) {
            const backendUrl = getApiUrl();
            let proxyUrl = '';
            let directUrl = '';
            
            // Handle different tool types
            if (tool === 'nifi') {
              // ports is an object like {"8080/tcp": [{"HostIp": "0.0.0.0", "HostPort": "9090"}]}
              const portMapping = container.ports['8080/tcp'];
              if (portMapping && portMapping.length > 0) {
                const hostPort = portMapping[0].HostPort;
                // Use nginx proxy that strips CSP headers and handles all NiFi resources
                // Nginx proxies /nifi/ to host machine's port 9090
                proxyUrl = `/nifi/`;
                directUrl = `http://localhost:${hostPort}/nifi/`;
              }
            } else if (tool === 'datahub') {
              const portMapping = container.ports['9002/tcp'];
              if (portMapping && portMapping.length > 0) {
                const hostPort = portMapping[0].HostPort;
                proxyUrl = `${backendUrl}/api/proxy/${containerId}/`;
                directUrl = `http://localhost:${hostPort}`;
              }
            } else if (tool === 'kafka') {
              const portMapping = container.ports['9092/tcp'];
              if (portMapping && portMapping.length > 0) {
                const hostPort = portMapping[0].HostPort;
                proxyUrl = `${backendUrl}/api/proxy/${containerId}/`;
                directUrl = `http://localhost:${hostPort}`;
              }
            } else if (tool === 'hue') {
              // Hue SQL Editor on port 8888
              const portMapping = container.ports['8888/tcp'];
              if (portMapping && portMapping.length > 0) {
                const hostPort = portMapping[0].HostPort;
                proxyUrl = `${backendUrl}/api/proxy/${containerId}/`;
                directUrl = `http://localhost:${hostPort}`;
              }
            } else {
              // Generic fallback - find first available port
              for (const [portKey, portValue] of Object.entries(container.ports)) {
                if (portValue && Array.isArray(portValue) && portValue.length > 0) {
                  const hostPort = (portValue as any)[0].HostPort;
                  proxyUrl = `${backendUrl}/api/proxy/${containerId}/`;
                  directUrl = `http://localhost:${hostPort}`;
                  break;
                }
              }
            }
            
            if (proxyUrl && directUrl) {
              // Use proxy URL for iframe (strips CSP headers), keep direct URL for "Open External" button
              console.log('[ToolEmbed] Setting iframe URL to:', proxyUrl);
              console.log('[ToolEmbed] Direct URL for external:', directUrl);
              setIframeUrl(proxyUrl);  // Use proxy for iframe to avoid CSP issues
              setDirectUrl(directUrl);  // Keep direct URL for external link
              setLoading(false);
              
              // Update localStorage with correct port info
              localStorage.setItem(`container_${containerId}`, JSON.stringify({
                id: containerId,
                url: proxyUrl,
                directUrl: directUrl,
                updatedAt: new Date().toISOString()
              }));
              return;
            }
          }
        }
      } catch (err) {
        console.error('Failed to fetch container info:', err);
      }
      
      // Fallback: use proxy URL for both
      const backendUrl = getApiUrl();
      const toolPath = tool === 'nifi' ? '/nifi/' : '/';
      const proxyUrl = `${backendUrl}/api/proxy/${containerId}${toolPath}`;
      console.log('[ToolEmbed] Using fallback URL:', proxyUrl);
      setIframeUrl(proxyUrl);
      setDirectUrl(proxyUrl);
      setLoading(false);
    };
    
    loadContainerInfo();
  }, [containerId, tool]);

  const handleRefresh = () => {
    const iframe = document.getElementById('tool-iframe') as HTMLIFrameElement;
    if (iframe) {
      iframe.src = iframe.src;
    }
  };

  const handleOpenExternal = () => {
    if (directUrl) {
      window.open(directUrl, '_blank');
    }
  };

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center">
          <Loader2 className="w-12 h-12 text-cloudera-blue animate-spin mx-auto mb-4" />
          <p className="text-gray-600">Loading {tool}...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-600 mb-4">{error}</p>
          <button
            onClick={() => navigate(-1)}
            className="px-4 py-2 bg-cloudera-blue text-white rounded hover:bg-blue-700"
          >
            Go Back
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col bg-gray-100">
      {/* Toolbar */}
      <div className="bg-white border-b border-gray-200 px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate(-1)}
            className="p-1 hover:bg-gray-100 rounded"
            title="Close"
          >
            <X className="w-5 h-5" />
          </button>
          <div className="border-l border-gray-300 h-6 mx-2"></div>
          <h2 className="text-lg font-semibold">
            {tool?.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())}
          </h2>
          <span className="text-sm text-gray-500">({containerId?.substring(0, 12)})</span>
        </div>

        <div className="flex items-center gap-2">
          {tool === 'nifi' && (
            <button
              onClick={async () => {
                try {
                  // Option 1: Try to open the NiFi Instance container (top-level)
                  const nifiInstanceKey = `nifi-instance-${containerId}`;
                  const nifiInstanceUrn = `urn:li:container:${nifiInstanceKey}`;
                  
                  // Open DataHub showing the NiFi instance container
                  // This shows all process groups and processors in this NiFi instance
                  window.open(`${config.dataHubUrl}/container/${encodeURIComponent(nifiInstanceUrn)}/Contents?is_lineage_mode=false`, '_blank');
                  
                  // Note: If you want to view a specific process group instead:
                  // 1. Get the current process group ID from the iframe
                  // 2. Use: urn:li:container:nifi-pg-{containerId}-{processGroupId}
                } catch (error) {
                  console.error('Failed to open DataHub:', error);
                  // Fallback: Open DataHub search for this NiFi instance
                  const searchQuery = encodeURIComponent(containerId?.substring(0, 12) || '');
                  window.open(`${config.dataHubUrl}/search?filter_platform___false___EQUAL___0=nifi&query=${searchQuery}`, '_blank');
                }
              }}
              className="flex items-center gap-2 px-3 py-1.5 text-sm bg-cloudera-blue text-white rounded hover:bg-blue-700"
              title="View this NiFi instance in DataHub to see all processors, lineage, and metadata"
            >
              <Database className="w-4 h-4" />
              View in DataHub
            </button>
          )}
          <button
            onClick={handleRefresh}
            className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-50"
            title="Refresh"
            disabled={!showIframe}
          >
            <RefreshCw className="w-4 h-4" />
            Refresh
          </button>
          <button
            onClick={handleOpenExternal}
            className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-50"
            title="Open in new tab"
            disabled={!showIframe}
          >
            <ExternalLink className="w-4 h-4" />
            Open External
          </button>
          <button
            onClick={() => {
              const elem = document.documentElement;
              if (elem.requestFullscreen) {
                elem.requestFullscreen();
              }
            }}
            className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-50"
            title="Fullscreen"
          >
            <Maximize2 className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Progress Bar for Container Startup */}
      {!showIframe && (
        <div className="flex-1 flex items-center justify-center bg-gradient-to-br from-blue-50 to-teal-50">
          <div className="max-w-md w-full px-8">
            <div className="text-center mb-8">
              <div className="inline-flex items-center justify-center w-20 h-20 bg-cloudera-blue rounded-full mb-4">
                <Loader2 className="w-10 h-10 text-white animate-spin" />
              </div>
              <h3 className="text-2xl font-semibold text-gray-800 mb-2">
                Opening {tool?.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())} Container
              </h3>
              <p className="text-gray-600">{health.message || 'Loading interface...'}</p>
            </div>

            {/* Progress Bar */}
            <div className="relative">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-700">Progress</span>
                <span className="text-sm font-semibold text-cloudera-blue">{health.progress}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
                <div
                  className="bg-gradient-to-r from-cloudera-blue to-teal-500 h-3 rounded-full transition-all duration-500 ease-out"
                  style={{ width: `${health.progress}%` }}
                >
                  <div className="h-full w-full bg-white/20 animate-pulse"></div>
                </div>
              </div>
            </div>

            {/* Status Steps */}
            <div className="mt-8 space-y-3">
              <div className={`flex items-center gap-3 ${health.status === 'starting' ? 'text-cloudera-blue' : 'text-gray-400'}`}>
                <div className={`w-2 h-2 rounded-full ${health.status === 'starting' ? 'bg-cloudera-blue animate-pulse' : 'bg-gray-300'}`}></div>
                <span className="text-sm">Connecting to container...</span>
              </div>
              <div className={`flex items-center gap-3 ${health.status === 'initializing' ? 'text-cloudera-blue' : 'text-gray-400'}`}>
                <div className={`w-2 h-2 rounded-full ${health.status === 'initializing' ? 'bg-cloudera-blue animate-pulse' : 'bg-gray-300'}`}></div>
                <span className="text-sm">Loading services...</span>
              </div>
              <div className={`flex items-center gap-3 ${health.status === 'ready' ? 'text-green-600' : 'text-gray-400'}`}>
                <div className={`w-2 h-2 rounded-full ${health.status === 'ready' ? 'bg-green-600' : 'bg-gray-300'}`}></div>
                <span className="text-sm">Ready to use!</span>
              </div>
            </div>

            <div className="mt-8 text-center space-y-2">
              <p className="text-xs text-gray-500">
                {tool === 'nifi' ? 'NiFi UI typically takes a moment to load' : 'Loading container interface...'}
              </p>
              <p className="text-xs text-blue-600">
                💡 The page will load automatically - no need to refresh!
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Embedded Tool - Only show when ready */}
      {showIframe && (
        <div className="flex-1 relative">
          <iframe
            id="tool-iframe"
            src={iframeUrl}
            className="w-full h-full border-0"
            title={`${tool} Interface`}
            sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals allow-downloads"
          />
        </div>
      )}
    </div>
  );
};

export default ToolEmbed;
