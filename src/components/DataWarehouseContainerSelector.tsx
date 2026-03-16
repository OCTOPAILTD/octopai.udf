import { useState } from 'react';
import { X, Database, Loader2 } from 'lucide-react';
import { containerService } from '../services/containerService';
import ContainerCreationProgress from './ContainerCreationProgress';

interface DataWarehouseContainerSelectorProps {
  workspaceId: string;
  workspaceName: string;
  onClose: () => void;
  onContainerCreated: (container: any) => void;
}

const DataWarehouseContainerSelector = ({
  workspaceId,
  workspaceName,
  onClose,
  onContainerCreated,
}: DataWarehouseContainerSelectorProps) => {
  const [creating, setCreating] = useState<string | null>(null);
  const [showProgress, setShowProgress] = useState(false);
  const [progressContainerName, setProgressContainerName] = useState('');
  const [createdContainer, setCreatedContainer] = useState<any>(null);
  const [progressError, setProgressError] = useState<string | null>(null);

  const containerOptions = [
    {
      id: 'trino',
      name: 'Trino',
      description: 'Fast distributed SQL query engine for analytics',
      icon: Database,
    },
    {
      id: 'hive',
      name: 'Hive',
      description: 'Data warehouse with HiveServer2 and Hue SQL editor',
      icon: Database,
    },
    {
      id: 'impala',
      name: 'Impala',
      description: 'High-performance SQL query engine for Hadoop',
      icon: Database,
    },
    {
      id: 'hbase',
      name: 'HBase',
      description: 'NoSQL database for real-time read/write access',
      icon: Database,
    },
  ];

  const handleCreateContainer = async (containerType: string) => {
    setCreating(containerType);
    setProgressError(null);
    setShowProgress(true);
    setProgressContainerName(`${containerType}-${workspaceName}`);

    try {
      let container;
      const containerName = `${containerType}-${workspaceName}`;

      // Create a timeout promise
      const timeoutPromise = new Promise((_, reject) => 
        setTimeout(() => reject(new Error('Request timeout: Container creation took too long')), 60000)
      );

      // Create the actual container promise
      let createPromise;
      if (containerType === 'trino') {
        createPromise = containerService.createTrino(containerName, workspaceId);
      } else if (containerType === 'hive') {
        createPromise = containerService.createHive(containerName, workspaceId);
      } else if (containerType === 'impala') {
        createPromise = containerService.createImpala(containerName, workspaceId);
      } else if (containerType === 'hbase') {
        createPromise = containerService.createHBase(containerName, workspaceId);
      } else {
        throw new Error(`Unknown container type: ${containerType}`);
      }

      // Race between creation and timeout (2 minutes for image pull)
      container = await Promise.race([createPromise, timeoutPromise]) as any;

      if (!container || !container.id) {
        throw new Error('Container creation failed: No container ID returned');
      }

      // Store container info
      localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
      setCreatedContainer(container);
      
      // Callback to parent
      onContainerCreated(container);
    } catch (error: any) {
      let errorMessage = error.message || `Failed to create ${containerType} container`;
      
      // Provide helpful error messages
      if (errorMessage.includes('timeout') || errorMessage.includes('too long')) {
        errorMessage = `Container creation is taking longer than expected. This usually means the Docker image is being downloaded (this can take 2-5 minutes for large images like ${containerType}). The container may still be creating in the background. Please check Docker Desktop or try again in a few minutes.`;
      }
      
      setProgressError(errorMessage);
      setCreating(null);
      console.error(`Error creating ${containerType}:`, error);
      
      // Show error in progress modal
      setShowProgress(true);
    }
  };

  return (
    <>
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-white rounded-lg shadow-xl w-[600px] max-h-[80vh] overflow-y-auto">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-200">
            <div>
              <h2 className="text-xl font-semibold">Choose Your Data Warehouse Engine</h2>
              <p className="text-sm text-gray-600 mt-1">
                Select a container to create for workspace "{workspaceName}"
              </p>
            </div>
            <button
              onClick={onClose}
              className="p-1 hover:bg-gray-100 rounded"
              disabled={creating !== null}
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Container Options */}
          <div className="p-6">
            <div className="grid grid-cols-2 gap-4">
              {containerOptions.map((option) => (
                <button
                  key={option.id}
                  onClick={() => handleCreateContainer(option.id)}
                  disabled={creating !== null}
                  className={`flex flex-col items-center p-6 border-2 rounded-lg transition-all text-left ${
                    creating === option.id
                      ? 'border-cloudera-blue bg-blue-50'
                      : creating !== null
                      ? 'border-gray-200 opacity-50 cursor-not-allowed'
                      : 'border-gray-200 hover:border-cloudera-blue hover:shadow-md'
                  }`}
                >
                  {creating === option.id ? (
                    <Loader2 className="w-8 h-8 text-cloudera-blue animate-spin mb-3" />
                  ) : (
                    <div className="w-12 h-12 rounded-lg bg-gradient-to-br from-blue-500 to-green-500 flex items-center justify-center mb-3">
                      <option.icon className="w-6 h-6 text-white" />
                    </div>
                  )}
                  <h3 className="font-semibold text-lg mb-1">{option.name}</h3>
                  <p className="text-sm text-gray-600 text-center">{option.description}</p>
                </button>
              ))}
            </div>
          </div>

          {/* Footer */}
          <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
            <button
              onClick={onClose}
              disabled={creating !== null}
              className="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Skip for now
            </button>
          </div>
        </div>
      </div>

      {/* Progress Modal */}
      <ContainerCreationProgress
        isOpen={showProgress}
        containerName={progressContainerName}
        error={progressError}
        containerId={createdContainer?.id || null}
        onClose={(status) => {
          setShowProgress(false);
          setProgressError(null);
          setCreating(null);
          
          if (status === 'success' && createdContainer) {
            // Close both modals and navigate
            onClose();
            // Reload to show the new container
            setTimeout(() => window.location.reload(), 500);
          }
        }}
      />
    </>
  );
};

export default DataWarehouseContainerSelector;
