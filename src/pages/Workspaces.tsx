import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Search, X, Workflow, Activity, Database, Brain, Plus } from 'lucide-react';
import { containerService } from '../services/containerService';
import DataWarehouseContainerSelector from '../components/DataWarehouseContainerSelector';

const Workspaces = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const filterType = searchParams.get('type'); // Get filter from URL
  
  const [searchTerm, setSearchTerm] = useState('');
  const [showNewWorkspaceModal, setShowNewWorkspaceModal] = useState(false);
  const [newWorkspaceName, setNewWorkspaceName] = useState('');
  const [newWorkspaceTemplate, setNewWorkspaceTemplate] = useState(filterType || 'data-engineering');
  const [showContainerSelector, setShowContainerSelector] = useState(false);
  const [createdWorkspace, setCreatedWorkspace] = useState<{ id: string; name: string } | null>(null);

  // Load workspaces from localStorage and merge with defaults
  const loadWorkspaces = () => {
    const defaultWorkspaces = [
      { id: 'w1', name: 'DataEngineering1', icon: Workflow, shared: true, type: 'data-engineering' },
      { id: 'w2', name: 'StreamingAnalytics', icon: Activity, shared: false, type: 'streaming' },
      { id: 'w3', name: 'SQLWarehouse', icon: Database, shared: true, type: 'warehouse' },
      { id: 'w4', name: 'MLWorkspace', icon: Brain, shared: false, type: 'data-science' },
      { id: 'w5', name: 'KafkaConnect', icon: Workflow, shared: true, type: 'data-engineering' },
      { id: 'w6', name: 'FlinkProcessing', icon: Activity, shared: false, type: 'streaming' },
      { id: 'w7', name: 'ImpalaAnalytics', icon: Database, shared: true, type: 'warehouse' },
      { id: 'w8', name: 'SparkNotebooks', icon: Brain, shared: false, type: 'data-science' },
    ];

    // Load workspaces from localStorage
    const loadedWorkspaces: any[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith('workspace_')) {
        try {
          const workspaceData = JSON.parse(localStorage.getItem(key) || '{}');
          if (workspaceData.id && workspaceData.name) {
            // Find icon based on type
            let icon = Database;
            if (workspaceData.type === 'data-engineering') icon = Workflow;
            else if (workspaceData.type === 'streaming') icon = Activity;
            else if (workspaceData.type === 'warehouse') icon = Database;
            else if (workspaceData.type === 'data-science') icon = Brain;
            
            loadedWorkspaces.push({
              id: workspaceData.id,
              name: workspaceData.name,
              icon: icon,
              shared: false,
              type: workspaceData.type || 'data-engineering',
            });
          }
        } catch (e) {
          console.error('Error loading workspace:', e);
        }
      }
    }

    // Merge defaults with loaded workspaces (avoid duplicates)
    const allWorkspaces = [...defaultWorkspaces];
    loadedWorkspaces.forEach(loaded => {
      if (!allWorkspaces.find(w => w.id === loaded.id)) {
        allWorkspaces.push(loaded);
      }
    });

    return allWorkspaces;
  };

  const [workspaces, setWorkspaces] = useState(loadWorkspaces());

  // Reload workspaces when component mounts or filter changes
  useEffect(() => {
    setWorkspaces(loadWorkspaces());
  }, [filterType]);

  // Set default template based on filter
  useEffect(() => {
    if (filterType) {
      setNewWorkspaceTemplate(filterType);
    }
  }, [filterType]);

  const templates = [
    { id: 'data-engineering', name: 'Data Engineering', icon: Workflow },
    { id: 'streaming', name: 'Real-time Streaming', icon: Activity },
    { id: 'warehouse', name: 'Data Warehouse', icon: Database },
    { id: 'data-science', name: 'Data Science', icon: Brain },
  ];

  const handleCreateWorkspace = async () => {
    if (!newWorkspaceName.trim()) {
      alert('Please enter a workspace name');
      return;
    }

    const template = templates.find(t => t.id === newWorkspaceTemplate);
    const workspaceId = `w${workspaces.length + 1}`;
    const newWorkspace = {
      id: workspaceId,
      name: newWorkspaceName,
      icon: template?.icon || Workflow,
      shared: false,
      type: newWorkspaceTemplate, // Store workspace type
    };

    // Store workspace metadata in localStorage FIRST
    localStorage.setItem(`workspace_${workspaceId}`, JSON.stringify({
      id: workspaceId,
      name: newWorkspaceName,
      type: newWorkspaceTemplate,
      createdAt: new Date().toISOString(),
    }));

    // Update state
    setWorkspaces([...workspaces, newWorkspace]);
    setShowNewWorkspaceModal(false);
    const workspaceName = newWorkspaceName; // Save before clearing
    setNewWorkspaceName('');
    
    // If Data Warehouse template, show container selector
    if (newWorkspaceTemplate === 'warehouse') {
      setCreatedWorkspace({ id: workspaceId, name: workspaceName });
      setShowContainerSelector(true);
    } else {
      // For other templates, navigate directly
      console.log(`Navigating to workspace: /workspace/${workspaceId}`);
      navigate(`/workspace/${workspaceId}`);
    }
  };

  const filteredWorkspaces = workspaces.filter((w) => {
    const matchesSearch = w.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = !filterType || w.type === filterType;
    return matchesSearch && matchesType;
  });

  return (
    <div className="fixed right-0 top-12 w-96 h-[calc(100vh-48px)] bg-white border-l border-gray-200 shadow-lg z-50">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200">
        <div>
          <h2 className="text-lg font-semibold">
            {filterType === 'warehouse' ? 'Data Warehouse Workspaces' : 'Workspaces'}
          </h2>
          {filterType && (
            <p className="text-xs text-gray-500 mt-1">
              Showing {filterType === 'warehouse' ? 'Data Warehouse' : filterType} workspaces only
            </p>
          )}
        </div>
        <div className="flex gap-2">
          <button className="p-1 hover:bg-gray-100 rounded">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <rect x="3" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="3" width="7" height="7" rx="1" />
              <rect x="3" y="14" width="7" height="7" rx="1" />
              <rect x="14" y="14" width="7" height="7" rx="1" />
            </svg>
          </button>
          <button onClick={() => navigate(-1)} className="p-1 hover:bg-gray-100 rounded">
            <X className="w-5 h-5" />
          </button>
        </div>
      </div>

      {/* Search */}
      <div className="p-4">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue"
          />
        </div>
      </div>

      {/* Filter Badge */}
      {filterType && (
        <div className="px-4 py-2">
          <button
            onClick={() => navigate('/workspaces')}
            className="flex items-center gap-2 px-3 py-1.5 text-xs bg-blue-100 text-blue-700 rounded-full hover:bg-blue-200 transition-colors"
          >
            <X className="w-3 h-3" />
            Clear filter: {filterType === 'warehouse' ? 'Data Warehouse' : filterType}
          </button>
        </div>
      )}

      {/* My Workspace Section */}
      <div className="px-4 py-2 bg-gray-100">
        <div className="flex items-center gap-2 py-2 cursor-pointer hover:bg-gray-200 px-2 rounded">
          <div className="w-5 h-5 bg-cloudera-blue rounded flex items-center justify-center text-white text-xs">
            <Workflow className="w-3 h-3" />
          </div>
          <span className="text-sm font-medium">My workspace</span>
        </div>
      </div>

      {/* All Workspaces */}
      <div className="flex-1 overflow-y-auto">
        <div className="px-4 py-2 text-xs font-semibold text-gray-600">
          {filterType ? `${filterType === 'warehouse' ? 'Data Warehouse' : filterType} Workspaces` : 'All Workspaces'} ({filteredWorkspaces.length})
        </div>

        <div className="px-4">
          {filteredWorkspaces.map((workspace) => (
            <div
              key={workspace.id}
              onClick={() => navigate(`/workspace/${workspace.id}`)}
              className="flex items-center gap-2 py-2 cursor-pointer hover:bg-gray-100 px-2 rounded group"
            >
              <workspace.icon className="w-5 h-5 text-cloudera-blue" />
              <span className="text-sm flex-1">{workspace.name}</span>
              {workspace.shared && (
                <>
                  <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z" />
                  </svg>
                  <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" />
                  </svg>
                </>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Footer */}
      <div className="border-t border-gray-200 p-4">
        <button className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900 mb-3">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          Deployment pipelines
        </button>

        <button 
          onClick={() => setShowNewWorkspaceModal(true)}
          className="w-full flex items-center justify-center gap-2 py-2 bg-cloudera-green text-white rounded hover:bg-green-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New workspace
        </button>
      </div>

      {/* New Workspace Modal */}
      {showNewWorkspaceModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl w-[500px] max-h-[80vh] overflow-y-auto">
            {/* Modal Header */}
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-semibold">Create New Workspace</h2>
              <button 
                onClick={() => setShowNewWorkspaceModal(false)}
                className="p-1 hover:bg-gray-100 rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Content */}
            <div className="p-6">
              {/* Workspace Name */}
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Workspace Name *
                </label>
                <input
                  type="text"
                  value={newWorkspaceName}
                  onChange={(e) => setNewWorkspaceName(e.target.value)}
                  placeholder="e.g., My Data Engineering Project"
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue"
                  autoFocus
                />
              </div>

              {/* Template Selection */}
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 mb-3">
                  Choose Template
                </label>
                <div className="grid grid-cols-2 gap-3">
                  {templates.map((template) => (
                    <button
                      key={template.id}
                      onClick={() => setNewWorkspaceTemplate(template.id)}
                      className={`flex flex-col items-center p-4 border-2 rounded-lg transition-all ${
                        newWorkspaceTemplate === template.id
                          ? 'border-cloudera-blue bg-blue-50'
                          : 'border-gray-200 hover:border-gray-300'
                      }`}
                    >
                      <template.icon className={`w-8 h-8 mb-2 ${
                        newWorkspaceTemplate === template.id ? 'text-cloudera-blue' : 'text-gray-600'
                      }`} />
                      <span className="text-sm font-medium text-center">{template.name}</span>
                    </button>
                  ))}
                </div>
              </div>

              {/* Description */}
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Description (optional)
                </label>
                <textarea
                  placeholder="Describe what this workspace is for..."
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue h-20 resize-none"
                />
              </div>
            </div>

            {/* Modal Footer */}
            <div className="flex items-center justify-end gap-3 p-6 border-t border-gray-200 bg-gray-50">
              <button
                onClick={() => setShowNewWorkspaceModal(false)}
                className="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-100"
              >
                Cancel
              </button>
              <button
                onClick={handleCreateWorkspace}
                className="px-4 py-2 text-sm bg-cloudera-green text-white rounded hover:bg-green-600"
              >
                Create Workspace
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Data Warehouse Container Selector */}
      {showContainerSelector && createdWorkspace && (
        <DataWarehouseContainerSelector
          workspaceId={createdWorkspace.id}
          workspaceName={createdWorkspace.name}
          onClose={() => {
            setShowContainerSelector(false);
            setCreatedWorkspace(null);
            // Navigate to workspace after closing selector
            navigate(`/workspace/${createdWorkspace.id}`);
          }}
          onContainerCreated={(container) => {
            console.log('Container created:', container);
            // Container info is already stored in localStorage by the selector
          }}
        />
      )}
    </div>
  );
};

export default Workspaces;

