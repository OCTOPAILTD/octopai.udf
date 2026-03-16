import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { X, Search, Star, Workflow, Activity, Database, FileText, Table, Zap, GitBranch, Loader2, Server } from 'lucide-react';
import { containerService } from '../services/containerService';
import ContainerCreationProgress from './ContainerCreationProgress';

interface NewItemPanelProps {
  onClose: () => void;
  workspaceType?: string; // Optional workspace type to filter items
}

interface Item {
  id: string;
  name: string;
  description: string;
  icon: any;
  category: 'get-data' | 'store-data' | 'transform' | 'streaming' | 'analytics';
  tool: string;
}

const NewItemPanel = ({ onClose, workspaceType }: NewItemPanelProps) => {
  const navigate = useNavigate();
  const { id: workspaceId } = useParams<{ id: string }>();
  const [activeTab, setActiveTab] = useState<'all' | 'favorites'>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [creating, setCreating] = useState<string | null>(null);
  const [createdContainer, setCreatedContainer] = useState<any>(null);
  const [showProgress, setShowProgress] = useState(false);
  const [progressContainerName, setProgressContainerName] = useState('');
  const [progressError, setProgressError] = useState<string | null>(null);

  const items: Item[] = [
    {
      id: 'nifi-flow',
      name: 'NiFi Flow',
      description: 'Create data ingestion and ETL pipelines with Apache NiFi',
      icon: Workflow,
      category: 'get-data',
      tool: 'Apache NiFi',
    },
    {
      id: 'kafka-topic',
      name: 'Kafka Topic',
      description: 'Create a new Kafka topic for streaming data',
      icon: Activity,
      category: 'streaming',
      tool: 'Apache Kafka',
    },
    {
      id: 'kafka-connect',
      name: 'Kafka Connect',
      description: 'Set up Kafka Connect for data integration',
      icon: GitBranch,
      category: 'get-data',
      tool: 'Kafka Connect',
    },
    {
      id: 'flink-job',
      name: 'Flink Job',
      description: 'Create real-time stream processing with Apache Flink',
      icon: Zap,
      category: 'streaming',
      tool: 'Apache Flink',
    },
    {
      id: 'impala-table',
      name: 'Impala Table',
      description: 'Create warehouse tables for SQL analytics',
      icon: Table,
      category: 'store-data',
      tool: 'Apache Impala',
    },
    {
      id: 'hive-table',
      name: 'Hive Table',
      description: 'Create Hive tables for data warehouse',
      icon: Database,
      category: 'store-data',
      tool: 'Apache Hive',
    },
    {
      id: 'hive-workspace',
      name: 'Hive Workspace',
      description: 'Hive server with built-in Hue SQL editor',
      icon: Server,
      category: 'analytics',
      tool: 'Apache Hive + Hue',
    },
    {
      id: 'hue-query',
      name: 'SQL Query',
      description: 'Interactive SQL queries with Hue editor',
      icon: FileText,
      category: 'analytics',
      tool: 'Hue',
    },
    {
      id: 'spark-notebook',
      name: 'Spark Notebook',
      description: 'Explore, analyze, and build ML models with Apache Spark',
      icon: FileText,
      category: 'analytics',
      tool: 'Apache Spark',
    },
    {
      id: 'ozone-bucket',
      name: 'Ozone Bucket',
      description: 'Create object storage buckets in Apache Ozone',
      icon: Database,
      category: 'store-data',
      tool: 'Apache Ozone',
    },
    {
      id: 'hbase-table',
      name: 'HBase Table',
      description: 'Create NoSQL tables with Apache HBase',
      icon: Table,
      category: 'store-data',
      tool: 'Apache HBase',
    },
    {
      id: 'trino',
      name: 'Trino',
      description: 'Fast distributed SQL query engine for analytics',
      icon: Database,
      category: 'store-data',
      tool: 'Trino',
    },
    {
      id: 'datahub-instance',
      name: 'DataHub Metadata',
      description: 'Data catalog with lineage tracking and discovery',
      icon: Database,
      category: 'analytics',
      tool: 'DataHub',
    },
  ];

  const categories = [
    { id: 'get-data', label: 'Get data', description: 'Ingest batch and real-time data' },
    { id: 'streaming', label: 'Streaming', description: 'Real-time data processing' },
    { id: 'store-data', label: 'Store data', description: 'Data warehouse and storage' },
    { id: 'transform', label: 'Transform', description: 'Data transformation and ETL' },
    { id: 'analytics', label: 'Analytics', description: 'SQL and data science' },
  ];

  // Filter items based on workspace type
  const getItemsForWorkspaceType = () => {
    if (workspaceType === 'warehouse') {
      // For DataWarehouse workspace, show only: Trino, Hive, Impala, HBase
      return items.filter(item => 
        item.id === 'trino' || 
        item.id === 'hive-table' || 
        item.id === 'impala-table' || 
        item.id === 'hbase-table'
      );
    }
    // For other workspace types, show all items
    return items;
  };

  const filteredItems = getItemsForWorkspaceType().filter((item) => {
    const matchesSearch = item.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         item.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = !selectedCategory || item.category === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  const handleCreateItem = async (itemId: string, itemName: string) => {
    setCreating(itemId);
    setCreatedContainer(null);
    setProgressError(null);

    try {
      let container;
      const workspace = workspaceId || 'default'; // Use workspace ID from URL

      if (itemId === 'nifi-flow') {
        // Show progress modal immediately for user feedback
        setProgressContainerName(itemName);
        setShowProgress(true);
        
        try {
          // Create container (this takes ~2-3 seconds)
          container = await containerService.createNiFi(itemName, workspace);
          
          // Store container info in localStorage for the embed page
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          
          // Update modal with container ID to start streaming logs
          setCreatedContainer(container);
          
          // Progress modal will handle streaming and navigation when ready
        } catch (nifiError: any) {
          // Show error in progress modal
          setProgressError(nifiError.message || 'Failed to create NiFi container');
          setCreating(null);
          return;
        }
        
      } else if (itemId === 'kafka-topic') {
        container = await containerService.createKafka(itemName, workspace);
        
        // Store container info
        localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
        
        alert(`✅ Kafka container created!\n\nName: ${container.name}\nBroker: ${container.broker}`);
        
      } else if (itemId === 'datahub-instance') {
        container = await containerService.createDataHub(itemName, workspace);
        
        // Store container info in localStorage for the embed page
        localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
        
        // Close the panel
        onClose();
        
        // Give a moment for the container to register, then reload to show it
        setTimeout(() => window.location.reload(), 1000);
        
        // Navigate to embedded DataHub view
        navigate(`/tool/datahub/${container.id}`);
        
        // Show success message
        setTimeout(() => {
          alert(`✅ DataHub container created!\n\nName: ${container.name}\n\nDataHub is starting... please wait 2-3 minutes for all services to be ready.`);
        }, 500);
        
      } else if (itemId === 'trino') {
        // Show progress modal immediately
        setProgressContainerName(itemName);
        setShowProgress(true);
        
        try {
          container = await containerService.createTrino(itemName, workspace);
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          setCreatedContainer(container);
        } catch (trinoError: any) {
          setProgressError(trinoError.message || 'Failed to create Trino container');
          setCreating(null);
          return;
        }
      } else if (itemId === 'hive-table') {
        // Show progress modal immediately
        setProgressContainerName(itemName);
        setShowProgress(true);
        
        try {
          container = await containerService.createHive(itemName, workspace);
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          setCreatedContainer(container);
        } catch (hiveError: any) {
          setProgressError(hiveError.message || 'Failed to create Hive container');
          setCreating(null);
          return;
        }
      } else if (itemId === 'impala-table') {
        // Show progress modal immediately
        setProgressContainerName(itemName);
        setShowProgress(true);
        
        try {
          container = await containerService.createImpala(itemName, workspace);
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          setCreatedContainer(container);
        } catch (impalaError: any) {
          setProgressError(impalaError.message || 'Failed to create Impala container');
          setCreating(null);
          return;
        }
      } else if (itemId === 'hbase-table') {
        // Show progress modal immediately
        setProgressContainerName(itemName);
        setShowProgress(true);
        
        try {
          container = await containerService.createHBase(itemName, workspace);
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          setCreatedContainer(container);
        } catch (hbaseError: any) {
          setProgressError(hbaseError.message || 'Failed to create HBase container');
          setCreating(null);
          return;
        }
      } else if (itemId === 'hive-workspace') {
        try {
          container = await containerService.createHive(itemName, workspace);
          
          // Store container info in localStorage for the embed page
          localStorage.setItem(`container_${container.id}`, JSON.stringify(container));
          
          // Close the panel
          onClose();
          
          // Give a moment for the container to register, then reload to show it
          setTimeout(() => window.location.reload(), 1000);
          
          // Navigate to embedded Hue view
          navigate(`/tool/hue/${container.id}`);
          
          // Show success message with credentials
          setTimeout(() => {
            alert(`✅ Hive + Hue workspace created!\n\nName: ${container.name}\n\nHue SQL Editor: ${container.directUrl}\nUsername: ${container.credentials?.username || 'admin'}\nPassword: ${container.credentials?.password || 'admin'}\n\nHive is starting... please wait 1-2 minutes for services to be ready.`);
          }, 500);
        } catch (hiveError: any) {
          alert(`❌ Error creating Hive workspace: ${hiveError.message}`);
          console.error('Hive creation error:', hiveError);
          setCreating(null);
          return;
        }
        
      } else {
        alert(`Creating ${itemName}... (Container integration coming soon)`);
        setCreating(null);
        return;
      }

      setCreatedContainer(container);
    } catch (error: any) {
      alert(`❌ Error creating container: ${error.message}`);
      console.error('Error:', error);
    } finally {
      setCreating(null);
    }
  };

  return (
    <div className="fixed right-0 top-12 w-[800px] h-[calc(100vh-48px)] bg-white border-l border-gray-200 shadow-2xl z-50 flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200">
        <h2 className="text-xl font-semibold">New item</h2>
        <div className="flex items-center gap-4">
          <input
            type="text"
            placeholder="Filter by item type"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue w-64"
          />
          <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded">
            <X className="w-5 h-5" />
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-6 px-6 pt-4 border-b border-gray-200">
        <button
          onClick={() => setActiveTab('favorites')}
          className={`pb-2 text-sm font-medium flex items-center gap-2 ${
            activeTab === 'favorites'
              ? 'border-b-2 border-gray-900 text-gray-900'
              : 'text-gray-600 hover:text-gray-900'
          }`}
        >
          <Star className="w-4 h-4" />
          Favorites
        </button>
        <button
          onClick={() => setActiveTab('all')}
          className={`pb-2 text-sm font-medium flex items-center gap-2 ${
            activeTab === 'all'
              ? 'border-b-2 border-gray-900 text-gray-900'
              : 'text-gray-600 hover:text-gray-900'
          }`}
        >
          All items
        </button>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        {/* Categories */}
        {categories.map((category) => {
          const categoryItems = filteredItems.filter((item) => item.category === category.id);
          if (categoryItems.length === 0) return null;

          return (
            <div key={category.id} className="mb-6">
              <div className="px-6 py-3 bg-gray-50">
                <h3 className="font-semibold text-sm">{category.label}</h3>
                <p className="text-xs text-gray-600">{category.description}</p>
              </div>

              <div className="grid grid-cols-3 gap-4 p-6">
                {categoryItems.map((item) => (
                  <button
                    key={item.id}
                    onClick={() => handleCreateItem(item.id, item.name)}
                    disabled={creating === item.id}
                    className="flex flex-col items-start p-4 bg-white border border-gray-200 rounded-lg hover:border-cloudera-blue hover:shadow-md transition-all text-left group disabled:opacity-50 disabled:cursor-not-allowed relative"
                  >
                    {creating === item.id && (
                      <div className="absolute inset-0 bg-white/80 flex items-center justify-center rounded-lg">
                        <Loader2 className="w-6 h-6 text-cloudera-blue animate-spin" />
                      </div>
                    )}
                    <div className="flex items-start justify-between w-full mb-3">
                      <div className="w-10 h-10 rounded bg-gradient-to-br from-blue-500 to-green-500 flex items-center justify-center">
                        <item.icon className="w-5 h-5 text-white" />
                      </div>
                      <Star className="w-4 h-4 text-gray-300 group-hover:text-yellow-400 cursor-pointer" />
                    </div>
                    <h4 className="font-semibold text-sm mb-1">{item.name}</h4>
                    <p className="text-xs text-gray-600 mb-2">{item.description}</p>
                    <div className="text-xs text-gray-500 mt-auto">
                      {item.tool}
                    </div>
                  </button>
                ))}
              </div>
            </div>
          );
        })}

        {filteredItems.length === 0 && (
          <div className="flex flex-col items-center justify-center h-64 text-gray-500">
            <Search className="w-12 h-12 mb-4" />
            <p>No items found matching your search</p>
          </div>
        )}
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
            // Close panel and reload workspace to show new container
            onClose();
            window.location.reload();
          }
        }}
      />
    </div>
  );
};

export default NewItemPanel;
