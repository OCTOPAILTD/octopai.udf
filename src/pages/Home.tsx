import { useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import {
  Plus,
  Workflow,
  Database,
  Gauge,
  Activity,
  Table,
  Brain,
  Layers,
  GitBranch,
  ChevronRight,
  ExternalLink,
  GitGraph,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import { containerService } from '../services/containerService';

interface WorkspaceTemplate {
  id: string;
  name: string;
  icon: any;
  description: string;
}

interface LearningCard {
  title: string;
  description: string;
  image: string;
}

const Home = () => {
  const navigate = useNavigate();
  const [datahubRunning, setDatahubRunning] = useState(false);
  const [checkingDatahub, setCheckingDatahub] = useState(true);

  // Check if DataHub is running
  useEffect(() => {
    const checkDataHub = async () => {
      try {
        const containers = await containerService.listContainers();
        const datahubContainer = containers.find(c => c.image.includes('datahub'));
        setDatahubRunning(!!datahubContainer && datahubContainer.status === 'running');
      } catch (error) {
        console.error('Failed to check DataHub status:', error);
        setDatahubRunning(false);
      } finally {
        setCheckingDatahub(false);
      }
    };

    checkDataHub();
    // Check every 10 seconds
    const interval = setInterval(checkDataHub, 10000);
    return () => clearInterval(interval);
  }, []);

  const handleOpenDataHub = () => {
    navigate('/catalog');
  };

  const templates: WorkspaceTemplate[] = [
    {
      id: 'new',
      name: 'Workspaces',
      icon: Plus,
      description: 'Create a blank workspace',
    },
    {
      id: 'data-engineering',
      name: 'Data Engineering',
      icon: Workflow,
      description: 'ETL pipelines with Apache NiFi',
    },
    {
      id: 'streaming',
      name: 'Real-time Streaming',
      icon: Activity,
      description: 'Kafka and Flink processing',
    },
    {
      id: 'warehouse',
      name: 'Data Warehouse',
      icon: Database,
      description: 'Impala and Hive analytics',
    },
    {
      id: 'sql-analytics',
      name: 'SQL Analytics',
      icon: Table,
      description: 'Interactive queries with Hue',
    },
    {
      id: 'data-science',
      name: 'Data Science',
      icon: Brain,
      description: 'Spark ML and notebooks',
    },
    {
      id: 'lakehouse',
      name: 'Lakehouse',
      icon: Layers,
      description: 'Ozone storage and HBase',
    },
    {
      id: 'kafka-connect',
      name: 'Data Integration',
      icon: GitBranch,
      description: 'Kafka Connect pipelines',
    },
  ];

  const learningCards: LearningCard[] = [
    {
      title: 'Build a data pipeline',
      description: 'Complete an end-to-end tutorial with Apache NiFi',
      image: '🔄',
    },
    {
      title: 'Stream processing with Flink',
      description: 'Learn real-time analytics with Apache Flink',
      image: '⚡',
    },
    {
      title: 'Build a data lakehouse',
      description: 'Complete an end-to-end tutorial with Ozone',
      image: '🏛️',
    },
    {
      title: 'SQL analytics workspace',
      description: 'Query data with Impala and Hive',
      image: '📊',
    },
  ];

  const recentWorkspaces = [
    { name: 'DataEngineering1', timestamp: '4 hours ago', icon: Workflow },
    { name: 'StreamingAnalytics', timestamp: '21 hours ago', icon: Activity },
    { name: 'SQLWarehouse', timestamp: 'a day ago', icon: Database },
    { name: 'MLWorkspace', timestamp: '6 days ago', icon: Brain },
  ];

  return (
    <div className="p-8 max-w-[1600px] mx-auto">
      {/* Welcome Section */}
      <div className="mb-8">
        <h1 className="text-3xl font-semibold mb-2">Welcome to Cloudera Fabric Studio</h1>
        <p className="text-gray-600">
          Create a workspace with a predesigned template called a task flow. Task flows keep your items organized.{' '}
          <a href="#" className="text-cloudera-blue hover:underline">
            Learn more ↗
          </a>
        </p>
      </div>

      {/* DataHub Quick Access Banner */}
      <div className="mb-8">
        <div className="bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200 p-6 hover:shadow-lg transition-shadow">
          <div className="flex items-center justify-between">
            <div className="flex items-start gap-4">
              <div className="w-14 h-14 bg-white rounded-lg flex items-center justify-center shadow-sm">
                <GitGraph className="w-8 h-8 text-cloudera-blue" />
              </div>
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="text-xl font-semibold text-gray-900">DataHub Metadata Platform</h3>
                  {!checkingDatahub && (
                    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${
                      datahubRunning 
                        ? 'bg-green-100 text-green-700' 
                        : 'bg-gray-100 text-gray-600'
                    }`}>
                      {datahubRunning ? (
                        <>
                          <CheckCircle className="w-3.5 h-3.5" />
                          Running
                        </>
                      ) : (
                        <>
                          <XCircle className="w-3.5 h-3.5" />
                          Not Running
                        </>
                      )}
                    </span>
                  )}
                </div>
                <p className="text-gray-700 mb-3">
                  Explore data lineage, discover datasets, and track metadata across your entire data ecosystem. 
                  Real-time monitoring of NiFi flows with column-level lineage tracking.
                </p>
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <span className="flex items-center gap-1">
                    <Database className="w-4 h-4" />
                    Data Catalog
                  </span>
                  <span className="text-gray-400">•</span>
                  <span className="flex items-center gap-1">
                    <GitGraph className="w-4 h-4" />
                    Lineage Tracking
                  </span>
                  <span className="text-gray-400">•</span>
                  <span className="flex items-center gap-1">
                    <Activity className="w-4 h-4" />
                    Real-time Monitoring
                  </span>
                </div>
              </div>
            </div>
            <div className="flex flex-col gap-2">
              {datahubRunning ? (
                <>
                <button
                  onClick={handleOpenDataHub}
                  className="px-6 py-3 bg-cloudera-blue text-white rounded-lg hover:bg-blue-700 flex items-center gap-2 font-medium shadow-sm transition-all"
                >
                    Browse Data Catalog
                    <ChevronRight className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => window.open('http://localhost:9002', '_blank')}
                    className="px-6 py-3 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 flex items-center gap-2 text-sm transition-all"
                  >
                    Open DataHub UI
                  <ExternalLink className="w-4 h-4" />
                </button>
                </>
              ) : (
                <div className="text-center">
                  <p className="text-sm text-gray-600 mb-2">Start DataHub with:</p>
                  <code className="px-3 py-1.5 bg-white border border-gray-300 rounded text-xs font-mono block mb-2">
                    datahub docker quickstart
                  </code>
                  <p className="text-xs text-gray-500">Then browse the catalog above</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Workspace Templates */}
      <div className="mb-12">
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-4">
          {templates.map((template) => (
            <button
              key={template.id}
              onClick={() => {
                // Store template type for workspace creation
                if (template.id !== 'new') {
                  sessionStorage.setItem('selectedTemplate', template.id);
                  // Navigate with filter for Data Warehouse
                  if (template.id === 'warehouse') {
                    navigate('/workspaces?type=warehouse');
                  } else {
                    navigate('/workspaces');
                  }
                } else {
                  navigate('/workspaces');
                }
              }}
              className="flex flex-col items-center p-4 bg-white rounded-lg border border-gray-200 hover:border-cloudera-blue hover:shadow-md transition-all group"
            >
              <div className="w-12 h-12 mb-3 flex items-center justify-center rounded-lg bg-gray-50 group-hover:bg-blue-50 transition-colors">
                <template.icon className="w-6 h-6 text-gray-700 group-hover:text-cloudera-blue" />
              </div>
              <div className="text-sm font-medium text-center">{template.name}</div>
            </button>
          ))}
        </div>
      </div>

      {/* Learn More Section */}
      <div className="mb-12">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold">Learn more about Cloudera Fabric Studio</h2>
          <div className="flex gap-2">
            <button className="p-1 hover:bg-gray-200 rounded">
              <ChevronRight className="w-5 h-5 rotate-180" />
            </button>
            <button className="p-1 hover:bg-gray-200 rounded">
              <ChevronRight className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {learningCards.map((card, index) => (
            <div
              key={index}
              className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow cursor-pointer"
            >
              <h3 className="text-base font-semibold mb-2">{card.title}</h3>
              <p className="text-sm text-gray-600 mb-4">{card.description}</p>
              <div className="flex items-center justify-center h-32 bg-gradient-to-br from-blue-50 to-green-50 rounded-lg text-5xl">
                {card.image}
              </div>
            </div>
          ))}
        </div>

        <button className="mt-4 text-sm text-gray-600 hover:text-gray-900 flex items-center gap-1">
          Show less
          <ChevronRight className="w-4 h-4 rotate-90" />
        </button>
      </div>

      {/* Quick Access Section */}
      <div>
        <h2 className="text-xl font-semibold mb-4">Quick access</h2>

        <div className="bg-white rounded-lg border border-gray-200">
          {/* Tabs */}
          <div className="flex gap-6 px-4 pt-4 border-b border-gray-200">
            <button className="pb-2 text-sm font-medium border-b-2 border-gray-900">
              Recent workspaces
            </button>
            <button className="pb-2 text-sm text-gray-600 hover:text-gray-900">
              Recent items
            </button>
            <button className="pb-2 text-sm text-gray-600 hover:text-gray-900">
              Favorites
            </button>
          </div>

          {/* Search and Filter */}
          <div className="flex items-center justify-end gap-2 px-4 py-3">
            <input
              type="text"
              placeholder="Filter by keyword"
              className="px-3 py-1.5 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue"
            />
            <button className="px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-50 flex items-center gap-1">
              <Gauge className="w-4 h-4" />
              Filter
            </button>
          </div>

          {/* Workspace List */}
          <div className="divide-y divide-gray-200">
            <div className="grid grid-cols-[1fr_200px] gap-4 px-4 py-2 text-sm font-medium text-gray-600 bg-gray-50">
              <div>Name</div>
              <div>Opened</div>
            </div>

            {recentWorkspaces.map((workspace, index) => (
              <div
                key={index}
                className="grid grid-cols-[1fr_200px] gap-4 px-4 py-3 hover:bg-gray-50 cursor-pointer"
                onClick={() => navigate('/workspace/1')}
              >
                <div className="flex items-center gap-2">
                  <workspace.icon className="w-5 h-5 text-cloudera-blue" />
                  <span className="text-sm">{workspace.name}</span>
                </div>
                <div className="text-sm text-gray-600">{workspace.timestamp}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Home;

