import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Plus,
  FolderOpen,
  Upload,
  GitBranch,
  Filter,
  Edit,
  Workflow,
  Activity,
  ExternalLink,
  GitGraph,
  Play,
  Square,
  Trash2,
  MoreVertical,
  X,
  CheckCircle,
  Clock,
  Loader2,
  ScrollText,
  RefreshCw,
  Info,
  Database,
  Brain,
} from 'lucide-react';
import NewItemPanel from '../components/NewItemPanel';
import { containerService, type Container } from '../services/containerService';
import config from '../config';

interface WorkspaceItem {
  id: string;
  name: string;
  type: string;
  location: string;
  status?: string;
  icon: any;
}
const WorkspaceCanvas = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [showNewItem, setShowNewItem] = useState(false);
  const [selectedFilter, setSelectedFilter] = useState<string | null>(null);
  const [containers, setContainers] = useState<Container[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'items' | 'lineage'>('items');
  const [actionMenuOpen, setActionMenuOpen] = useState<string | null>(null);
  const [menuPosition, setMenuPosition] = useState<{ top: number; left: number } | null>(null);
  const [containerHealth, setContainerHealth] = useState<Record<string, { status: string; ready: boolean; nifi_ready?: boolean }>>({});
  const [notification, setNotification] = useState<{ show: boolean; message: string; containerId?: string } | null>(null);
  const [confirmDialog, setConfirmDialog] = useState<{
    show: boolean;
    title: string;
    message: string;
    action: () => void;
  } | null>(null);
  const [workspaceName, setWorkspaceName] = useState<string>('Workspace');
  const [workspaceIcon, setWorkspaceIcon] = useState<any>(Workflow);
  const [workspaceType, setWorkspaceType] = useState<string | null>(null);

  // Fetch containers - reload when workspace ID changes
  useEffect(() => {
    loadContainers();
    const interval = setInterval(() => {
      loadContainers();
    }, 10000); // Refresh every 10 seconds
    return () => clearInterval(interval);
  }, [id]); // Reload when workspace ID changes

  // Load workspace info from localStorage
  useEffect(() => {
    if (id) {
      const workspaceData = localStorage.getItem(`workspace_${id}`);
      if (workspaceData) {
        try {
          const parsed = JSON.parse(workspaceData);
          setWorkspaceName(parsed.name || 'Workspace');
          setWorkspaceType(parsed.type || null);
          
          // Set icon based on workspace type
          if (parsed.type === 'warehouse') {
            setWorkspaceIcon(Database);
          } else if (parsed.type === 'streaming') {
            setWorkspaceIcon(Activity);
          } else if (parsed.type === 'data-science') {
            setWorkspaceIcon(Brain);
          } else {
            setWorkspaceIcon(Workflow);
          }
        } catch (e) {
          console.error('Error parsing workspace data:', e);
        }
      } else {
        // Fallback for default workspaces
        const defaultWorkspaces: Record<string, { name: string; icon: any; type: string }> = {
          'w1': { name: 'DataEngineering1', icon: Workflow, type: 'data-engineering' },
          'w2': { name: 'StreamingAnalytics', icon: Activity, type: 'streaming' },
          'w3': { name: 'SQLWarehouse', icon: Database, type: 'warehouse' },
          'w4': { name: 'MLWorkspace', icon: Brain, type: 'data-science' },
          'w5': { name: 'KafkaConnect', icon: Workflow, type: 'data-engineering' },
          'w6': { name: 'FlinkProcessing', icon: Activity, type: 'streaming' },
          'w7': { name: 'ImpalaAnalytics', icon: Database, type: 'warehouse' },
          'w8': { name: 'SparkNotebooks', icon: Brain, type: 'data-science' },
        };
        
        const defaultWorkspace = defaultWorkspaces[id];
        if (defaultWorkspace) {
          setWorkspaceName(defaultWorkspace.name);
          setWorkspaceIcon(defaultWorkspace.icon);
          setWorkspaceType(defaultWorkspace.type);
        }
      }
    }
  }, [id]);

  // Auto-open NewItemPanel for DataWarehouse workspaces
  useEffect(() => {
    const autoOpen = sessionStorage.getItem('autoOpenNewItem');
    if (autoOpen === 'true') {
      sessionStorage.removeItem('autoOpenNewItem');
      setShowNewItem(true);
    }
  }, [id]);

  // Check health for running containers
  useEffect(() => {
    const checkContainerHealth = async () => {
      for (const container of containers) {
        if (container.status === 'running') {
          try {
            const health = await containerService.checkHealth(container.id);
            const previousHealth = containerHealth[container.id];
            
            setContainerHealth(prev => ({
              ...prev,
              [container.id]: health
            }));

            // Show notification when container becomes ready
            const isNiFi = container.name.includes('nifi');
            const wasNotReady = previousHealth && !previousHealth.nifi_ready && !previousHealth.ready;
            const isNowReady = (isNiFi && health.nifi_ready) || (!isNiFi && health.ready);
            
            if (wasNotReady && isNowReady) {
              setNotification({
                show: true,
                message: `${container.name} is now ready!`,
                containerId: container.id
              });
              
              // Auto-hide notification after 5 seconds
              setTimeout(() => {
                setNotification(null);
              }, 5000);
            }
          } catch (error) {
            console.error(`Failed to check health for ${container.id}:`, error);
          }
        }
      }
    };

    if (containers.length > 0) {
      checkContainerHealth();
      const interval = setInterval(checkContainerHealth, 5000); // Check health every 5 seconds
      return () => clearInterval(interval);
    }
  }, [containers, containerHealth]);

  // Close action menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (actionMenuOpen && !(event.target as HTMLElement).closest('.action-menu-container')) {
        setActionMenuOpen(null);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [actionMenuOpen]);


  const loadContainers = async () => {
    try {
      // Add timeout to prevent infinite loading
      const timeoutPromise = new Promise<never>((_, reject) => 
        setTimeout(() => reject(new Error('Request timeout')), 10000)
      );
      
      const data = await Promise.race([
        containerService.listContainers(),
        timeoutPromise
      ]);
      
      // Get workspace type from localStorage
      const workspaceData = id ? localStorage.getItem(`workspace_${id}`) : null;
      let workspaceType = null;
      if (workspaceData) {
        try {
          const parsed = JSON.parse(workspaceData);
          workspaceType = parsed.type;
        } catch (e) {
          console.error('Error parsing workspace data:', e);
        }
      }
      
      // Fallback: Check workspace name for known warehouse workspaces
      if (!workspaceType && id) {
        // Default warehouse workspaces (w3, w7 based on the default list)
        if (id === 'w3' || id === 'w7') {
          workspaceType = 'warehouse';
        }
      }
      
      // Filter containers based on workspace ID first
      console.log('[WorkspaceCanvas] Filtering containers for workspace:', id);
      console.log('[WorkspaceCanvas] Total containers:', data.length);
      console.log('[WorkspaceCanvas] Container workspaces:', data.map(c => ({ name: c.name, workspace: c.workspace })));
      
      let userContainers = data.filter(c => {
        const matchesWorkspace = c.workspace === id;
        const notClouderaStudio = !c.name.includes('cloudera-fabric-studio');
        if (!matchesWorkspace) {
          console.log(`[WorkspaceCanvas] Container ${c.name} filtered out: workspace mismatch (${c.workspace} !== ${id})`);
        }
        return matchesWorkspace && notClouderaStudio;
      });
      
      console.log('[WorkspaceCanvas] After workspace filter:', userContainers.length, 'containers');
      
      // For Data Warehouse workspaces, show only Hive, Impala, Trino, NiFi containers
      if (workspaceType === 'warehouse') {
        userContainers = userContainers.filter(c => 
          c.name.includes('hive') || 
          c.name.includes('hue') || 
          c.name.includes('impala') || 
          c.name.includes('trino') ||
          c.name.includes('hbase') ||
          c.name.includes('nifi')
        );
      } else {
        // For other workspaces, show all user containers (NiFi, Kafka, etc.)
        const beforeNameFilter = userContainers.length;
        console.log(`[WorkspaceCanvas] Applying name filter to ${beforeNameFilter} containers...`);
        userContainers.forEach(c => {
          console.log(`[WorkspaceCanvas] Checking container: ${c.name}`);
        });
        userContainers = userContainers.filter(c => {
          const matches = c.name.includes('nifi') || 
            c.name.includes('kafka') || 
            c.name.includes('hive') || 
            c.name.includes('hue') ||
            c.name.includes('flink') ||
            c.name.includes('spark');
          console.log(`[WorkspaceCanvas] Container ${c.name}: ${matches ? 'PASS' : 'FILTERED OUT'}`);
          return matches;
        });
        console.log(`[WorkspaceCanvas] After name filter: ${userContainers.length} containers (from ${beforeNameFilter})`);
      }
      
      console.log(`[WorkspaceCanvas] Final result: ${userContainers.length} containers for workspace ${id} (from ${data.length} total)`);
      setContainers(userContainers);
      setLoading(false);
    } catch (error) {
      console.error('Failed to load containers:', error);
      // Show empty state even on error
      setContainers([]);
      setLoading(false);
    }
  };

  const handleContainerClick = (container: Container) => {
    // Get the container's port mapping
    const ports = container.ports;
    
    console.log('handleContainerClick called', { container, ports });
    
    if (container.name.includes('nifi')) {
      // Open NiFi directly in external URL
      // Handle both array format and object format
      let hostPort = null;
      if (Array.isArray(ports)) {
        const portMapping = ports.find((p: any) => p.PrivatePort === 8080);
        hostPort = portMapping?.PublicPort;
        console.log('Array format detected', { portMapping, hostPort });
      } else if (ports && ports['8080/tcp']) {
        hostPort = ports['8080/tcp'][0]?.HostPort;
        console.log('Object format detected', { hostPort });
      }
      
      if (hostPort) {
        const url = `http://localhost:${hostPort}/nifi/`;
        console.log('Opening NiFi URL:', url);
        window.open(url, '_blank');
      } else {
        console.error('No port mapping found for NiFi container');
      }
    } else if (container.name.includes('kafka')) {
      // For other tools, navigate to iframe view
      navigate(`/tool/kafka/${container.id}`);
    } else if (container.name.includes('hue')) {
      // Open Hue SQL Editor
      navigate(`/tool/hue/${container.id}`);
    } else if (container.name.includes('hive')) {
      // Open Hive (typically accessed via Hue, but handle if clicked directly)
      alert('Hive server is running. Access it via the Hue SQL editor or JDBC connection.');
    }
  };

  const handleStartContainer = async (containerId: string, containerName: string) => {
    try {
      await containerService.startContainer(containerId);
      await loadContainers();
      setActionMenuOpen(null);
    } catch (error) {
      alert(`Failed to start ${containerName}: ${error}`);
    }
  };

  const handleStopContainer = async (containerId: string, containerName: string) => {
    setConfirmDialog({
      show: true,
      title: 'Stop Container',
      message: `Are you sure you want to stop "${containerName}"? This will interrupt any running processes.`,
      action: async () => {
        try {
          await containerService.stopContainer(containerId);
          await loadContainers();
          setActionMenuOpen(null);
        } catch (error) {
          alert(`Failed to stop ${containerName}: ${error}`);
        }
      }
    });
  };

  const handleRemoveContainer = async (containerId: string, containerName: string) => {
    setConfirmDialog({
      show: true,
      title: 'Remove Container',
      message: `Are you sure you want to permanently remove "${containerName}"? This action cannot be undone and all data will be lost.`,
      action: async () => {
        try {
          await containerService.removeContainer(containerId);
          await loadContainers();
          setActionMenuOpen(null);
        } catch (error) {
          alert(`Failed to remove ${containerName}: ${error}`);
        }
      }
    });
  };

  const handleViewLogs = (containerId: string) => {
    // Open logs in new window or modal
    window.open(`/logs/${containerId}`, '_blank');
    setActionMenuOpen(null);
  };

  const handleSyncToDataHub = async (containerId: string, containerName: string) => {
    try {
      const response = await fetch(`/api/sync/${containerId}`, { method: 'POST' });
      if (response.ok) {
        alert(`Successfully synced ${containerName} to DataHub!`);
      } else {
        throw new Error('Sync failed');
      }
      setActionMenuOpen(null);
    } catch (error) {
      alert(`Failed to sync ${containerName}: ${error}`);
    }
  };

  const handleViewDetails = (container: Container) => {
    // Navigate to container details or open modal
    setActionMenuOpen(null);
    handleContainerClick(container);
  };

  const items: WorkspaceItem[] = containers.map(c => ({
    id: c.id,
    name: c.name,
    type: c.name.includes('nifi') ? 'NiFi Flow' : 
          c.name.includes('kafka') ? 'Kafka Topic' :
          c.name.includes('hue') ? 'Hue SQL Editor' :
          c.name.includes('hive') ? 'Hive Server' :
          c.name.includes('trino') ? 'Trino' :
          c.name.includes('impala') ? 'Impala' :
          c.name.includes('hbase') ? 'HBase' :
          c.name.includes('datahub') ? 'DataHub Metadata' :
          'Container',
    location: workspaceName,
    status: c.status,
    icon: c.name.includes('nifi') ? Workflow : 
          c.name.includes('kafka') ? Activity :
          c.name.includes('hue') || c.name.includes('hive') ? ScrollText :
          Workflow,
  }));

  // Count actual containers dynamically
  const pipelinesCount = containers.filter(c => c.name.includes('nifi')).length;
  const dataSourcesCount = containers.filter(c => 
    c.name.includes('kafka') || 
    c.name.includes('database') || 
    c.name.includes('mysql') || 
    c.name.includes('postgres')
  ).length;

  const folders = [
    { id: 'f1', name: 'Pipelines', count: pipelinesCount },
    { id: 'f2', name: 'Data Sources', count: dataSourcesCount },
  ];

  return (
    <div className="h-full flex flex-col">
      {/* Workspace Header */}
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            {React.createElement(workspaceIcon, { className: "w-6 h-6 text-cloudera-blue" })}
            <h1 className="text-xl font-semibold">{workspaceName}</h1>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex gap-6 border-b border-gray-200 -mb-[1px]">
          <button
            onClick={() => setActiveTab('items')}
            className={`pb-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${
              activeTab === 'items'
                ? 'border-cloudera-blue text-cloudera-blue'
                : 'border-transparent text-gray-600 hover:text-gray-900'
            }`}
          >
            <Workflow className="w-4 h-4" />
            Items
          </button>
          <button
            onClick={() => setActiveTab('lineage')}
            className={`pb-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${
              activeTab === 'lineage'
                ? 'border-cloudera-blue text-cloudera-blue'
                : 'border-transparent text-gray-600 hover:text-gray-900'
            }`}
          >
            <GitGraph className="w-4 h-4" />
            Data Lineage
          </button>
        </div>
      </div>

      {/* Action Buttons - Only show on Items tab */}
      {activeTab === 'items' && (
        <div className="bg-white border-b border-gray-200 px-6 py-3">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setShowNewItem(true)}
              className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded hover:bg-gray-50"
            >
              <Plus className="w-4 h-4" />
              New item
            </button>
            <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded hover:bg-gray-50">
              <FolderOpen className="w-4 h-4" />
              New folder
            </button>
            <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded hover:bg-gray-50">
              <Upload className="w-4 h-4" />
              Import
            </button>
            <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded hover:bg-gray-50">
              <GitBranch className="w-4 h-4" />
              Migrate
            </button>

            <div className="ml-auto">
              <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded hover:bg-gray-50">
                <Filter className="w-4 h-4" />
                Add
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Canvas Area */}
      <div className="flex-1 overflow-auto">
        {activeTab === 'lineage' ? (
          <div className="h-full bg-gray-50 p-6">
            <div className="bg-white rounded-lg border border-gray-200 p-8 h-full flex flex-col items-center justify-center">
              <GitGraph className="w-16 h-16 text-cloudera-blue mb-4" />
              <h3 className="text-xl font-semibold mb-2">Data Lineage Visualization</h3>
              <p className="text-gray-600 mb-6 text-center max-w-md">
                View column-level lineage for all NiFi flows in this workspace, powered by DataHub.
              </p>
              <div className="flex gap-3">
                <button
                  onClick={() => {
                    const nifiContainer = containers.find(c => c.name.includes('nifi'));
                    if (nifiContainer) {
                      navigate(`/lineage/${nifiContainer.id}`);
                    } else {
                      alert('No NiFi containers found. Please create a NiFi flow first.');
                    }
                  }}
                  className="px-6 py-3 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
                >
                  <GitGraph className="w-4 h-4" />
                  View Lineage
                </button>
                <button
                  onClick={() => window.open('http://localhost:9002', '_blank')}
                  className="px-6 py-3 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2"
                >
                  <ExternalLink className="w-4 h-4" />
                  Open DataHub
                </button>
              </div>
              <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-4 max-w-md">
                <p className="text-sm text-blue-900">
                  <strong>Real-time Monitoring:</strong> Changes to your NiFi flows are automatically tracked and synced to DataHub for lineage analysis.
                </p>
              </div>
            </div>
          </div>
        ) : (
          <div className="p-6">
        {/* Folders Section */}
        {folders.length > 0 && (
          <div className="mb-8">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Folders</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {folders.map((folder) => (
                <div
                  key={folder.id}
                  className="bg-white border-2 border-gray-200 rounded-lg p-4 hover:border-cloudera-blue cursor-pointer transition-all"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-2">
                        <FolderOpen className="w-5 h-5 text-blue-500" />
                        <h3 className="font-semibold">{folder.name}</h3>
                      </div>
                      <p className="text-sm text-gray-600">{folder.count} items</p>
                    </div>
                    <button className="p-1 hover:bg-gray-100 rounded">
                      <Edit className="w-4 h-4 text-gray-400" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Items Section */}
        <div>
          <div className="flex items-center mb-4">
            <div className="flex items-center gap-2">
              {React.createElement(workspaceIcon, { className: "w-5 h-5 text-cloudera-blue" })}
              <span className="font-semibold">{workspaceName}</span>
              <span className="text-gray-400">›</span>
              <button className="text-sm text-gray-600 hover:text-gray-900">Filtered results</button>
            </div>

            <div className="ml-auto flex items-center gap-2">
              <button className="px-3 py-1 text-sm bg-gray-100 rounded hover:bg-gray-200">
                Clear all
              </button>
              <span className="text-sm text-gray-600">Task:</span>
              <button
                onClick={() => setSelectedFilter(null)}
                className="px-3 py-1 text-sm bg-blue-50 text-cloudera-blue rounded flex items-center gap-1"
              >
                Store ✕
              </button>
            </div>
          </div>

          {/* Items Table */}
          <div className="bg-white border border-gray-200 rounded-lg">
            {loading ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-gray-500">Loading containers...</div>
              </div>
            ) : items.length === 0 ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-center text-gray-500">
                  <Workflow className="w-12 h-12 mx-auto mb-4 text-gray-400" />
                  <p className="mb-2">No items yet</p>
                  <p className="text-sm">Click "+ New item" to create your first NiFi Flow, Kafka Topic, or DataHub instance</p>
                </div>
              </div>
            ) : (
              <div className="overflow-x-auto">
              <table className="w-full table-auto">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Container ID
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {items.map((item) => (
                    <tr 
                      key={item.id} 
                      className="hover:bg-gray-50 cursor-pointer"
                      onClick={() => {
                        const container = containers.find(c => c.id === item.id);
                        if (container) handleContainerClick(container);
                      }}
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <item.icon className="w-5 h-5 text-cloudera-blue" />
                          <span className="text-sm font-medium">{item.name}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        {(() => {
                          const health = containerHealth[item.id];
                          const container = containers.find(c => c.id === item.id);
                          const isNiFi = container?.name.includes('nifi');
                          
                          if (item.status === 'running' && health) {
                            // Show "Ready" if health check indicates ready (for NiFi with nifi_ready, or for other containers with ready: true)
                            if ((isNiFi && health.nifi_ready) || (!isNiFi && health.ready)) {
                              return (
                                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                                  <CheckCircle className="w-3 h-3" />
                                  Ready
                                </span>
                              );
                            } else if (item.status === 'running') {
                              // Show "Starting..." if container is running but not ready yet
                              return (
                                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-700">
                                  <Loader2 className="w-3 h-3 animate-spin" />
                                  Starting...
                                </span>
                              );
                            }
                          }
                          
                          if (item.status === 'running') {
                            return (
                              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                                <Clock className="w-3 h-3" />
                                Running
                              </span>
                            );
                          }
                          
                          return (
                            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                              {item.status || 'Unknown'}
                            </span>
                          );
                        })()}
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-gray-600">{item.type}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-xs text-gray-500 font-mono">{item.id.substring(0, 12)}</span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2 flex-wrap">
                          {/* Open in UDF — primary green CTA for NiFi */}
                          {item.name.includes('nifi') && (item.status === 'running' || item.status?.includes('Up')) && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                navigate('/udf-catalog/search?platform=NiFi');
                              }}
                              className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-green-600 text-white hover:bg-green-700 rounded-md shadow-sm"
                            >
                              <Database className="w-4 h-4" />
                              Open in UDF
                            </button>
                          )}

                          {/* Open NiFi */}
                          {item.name.includes('nifi') && (item.status === 'running' || item.status?.includes('Up')) && (
                            <a
                              href="http://localhost:8080/nifi/"
                              target="_blank"
                              rel="noopener noreferrer"
                              onClick={(e) => e.stopPropagation()}
                              className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium border border-blue-300 text-blue-600 hover:bg-blue-50 rounded-md"
                            >
                              <ExternalLink className="w-4 h-4" />
                              Open NiFi
                            </a>
                          )}

                          {/* Open in UDF — for Trino */}
                          {item.name.includes('trino') && (item.status === 'running' || item.status?.includes('Up')) && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                navigate('/udf-catalog/search?platform=Trino');
                              }}
                              className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-green-600 text-white hover:bg-green-700 rounded-md shadow-sm"
                            >
                              <Database className="w-4 h-4" />
                              Open in UDF
                            </button>
                          )}

                          {/* Open Hue SQL Editor */}
                          {item.name.includes('hue') && item.status === 'running' && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                navigate(`/tool/hue/${item.id}`);
                              }}
                              className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium border border-green-300 text-green-600 hover:bg-green-50 rounded-md"
                            >
                              <ScrollText className="w-4 h-4" />
                              Open SQL Editor
                            </button>
                          )}

                          <div className="relative action-menu-container">
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                const rect = e.currentTarget.getBoundingClientRect();
                                setMenuPosition({
                                  top: rect.bottom + 4,
                                  left: rect.right - 192 // 192px = w-48
                                });
                                setActionMenuOpen(actionMenuOpen === item.id ? null : item.id);
                              }}
                              className="p-1 hover:bg-gray-100 rounded"
                            >
                              <MoreVertical className="w-4 h-4 text-gray-600" />
                            </button>
                          </div>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              </div>
            )}
          </div>
        </div>
          </div>
        )}
      </div>

      {/* New Item Panel */}
      {showNewItem && (
        <NewItemPanel 
          onClose={() => setShowNewItem(false)} 
          workspaceType={(() => {
            // Get workspace type from localStorage
            const workspaceData = localStorage.getItem(`workspace_${id}`);
            if (workspaceData) {
              try {
                const parsed = JSON.parse(workspaceData);
                return parsed.type;
              } catch (e) {
                return undefined;
              }
            }
            return undefined;
          })()}
        />
      )}

      {/* Confirmation Dialog */}
      {confirmDialog?.show && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl w-[450px]">
            <div className="p-6">
              <h3 className="text-lg font-semibold mb-2">{confirmDialog.title}</h3>
              <p className="text-gray-600 text-sm mb-6">{confirmDialog.message}</p>
              
              <div className="flex items-center justify-end gap-3">
                <button
                  onClick={() => setConfirmDialog(null)}
                  className="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-100"
                >
                  Cancel
                </button>
                <button
                  onClick={() => {
                    confirmDialog.action();
                    setConfirmDialog(null);
                  }}
                  className="px-4 py-2 text-sm bg-red-600 text-white rounded hover:bg-red-700"
                >
                  Confirm
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Click outside to close action menu */}
      {actionMenuOpen && (
        <div 
          className="fixed inset-0 z-0" 
          onClick={() => setActionMenuOpen(null)}
        />
      )}

      {/* Ready Notification Toast */}
      {notification?.show && (
        <div className="fixed bottom-6 right-6 z-50 animate-bounce" style={{ animation: 'bounce 1s ease-in-out 2' }}>
          <div className="bg-white rounded-lg shadow-2xl border-l-4 border-green-500 p-4 flex items-center gap-3 min-w-[300px]">
            <div className="flex-shrink-0">
              <CheckCircle className="w-6 h-6 text-green-500" />
            </div>
            <div className="flex-1">
              <p className="text-sm font-medium text-gray-900">Container Ready!</p>
              <p className="text-xs text-gray-600 mt-0.5">{notification.message}</p>
            </div>
            <button
              onClick={() => {
                if (notification.containerId) {
                  const container = containers.find(c => c.id === notification.containerId);
                  if (container) handleContainerClick(container);
                }
              }}
              className="flex-shrink-0 px-3 py-1 text-xs bg-green-500 text-white rounded hover:bg-green-600 transition-colors"
            >
              Open
            </button>
            <button
              onClick={() => setNotification(null)}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Fixed Action Menu Dropdown */}
      {actionMenuOpen && menuPosition && (() => {
        const container = containers.find(c => c.id === actionMenuOpen);
        if (!container) return null;
        return (
          <div 
            className="fixed w-48 bg-white border border-gray-200 rounded-lg shadow-xl z-[9999]"
            style={{ 
              top: `${menuPosition.top}px`, 
              left: `${menuPosition.left}px` 
            }}
          >
            {/* View Details */}
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleViewDetails(container);
              }}
              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-t-lg"
            >
              <Info className="w-4 h-4 text-blue-600" />
              View Details
            </button>

            {/* Start/Stop */}
            {container.status !== 'running' ? (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  handleStartContainer(container.id, container.name);
                }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                <Play className="w-4 h-4 text-green-600" />
                Start Container
              </button>
            ) : (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  handleStopContainer(container.id, container.name);
                }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                <Square className="w-4 h-4 text-orange-600" />
                Stop Container
              </button>
            )}

            {/* View Logs */}
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleViewLogs(container.id);
              }}
              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
            >
              <ScrollText className="w-4 h-4 text-gray-600" />
              View Logs
            </button>

            {/* Sync to DataHub */}
            {container.name.includes('nifi') && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  handleSyncToDataHub(container.id, container.name);
                }}
                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                disabled={container.status !== 'running'}
              >
                <RefreshCw className="w-4 h-4 text-blue-600" />
                Sync to DataHub
              </button>
            )}
            
            {/* Remove - Danger Zone */}
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleRemoveContainer(container.id, container.name);
              }}
              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50 border-t border-gray-100 rounded-b-lg"
            >
              <Trash2 className="w-4 h-4" />
              Remove Container
            </button>
          </div>
        );
      })()}
    </div>
  );
};

export default WorkspaceCanvas;
