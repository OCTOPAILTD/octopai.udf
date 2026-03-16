import { useState } from 'react';
import {
  Zap,
  Activity,
  TrendingUp,
  AlertTriangle,
  Eye,
  Database,
  Filter,
  Plus,
  Waves,
} from 'lucide-react';

interface StreamData {
  id: string;
  name: string;
  type: string;
  sourceItem: string;
  itemOwner: string;
  workspace: string;
  throughput: string;
  status: 'active' | 'idle' | 'error';
}

const RealTimeHub = () => {
  const [selectedTab, setSelectedTab] = useState<'hub' | 'sources' | 'events'>('hub');
  const [dataTypeFilter, setDataTypeFilter] = useState('all');

  const streamingData: StreamData[] = [
    {
      id: '1',
      name: 'eventStreamTemps-stream',
      type: 'Kafka Stream',
      sourceItem: 'eventStreamTemps',
      itemOwner: 'Data Engineering Team',
      workspace: 'RealTimeAnalytics',
      throughput: '1.2k msg/s',
      status: 'active',
    },
    {
      id: '2',
      name: 'tempTables',
      type: 'Flink Stream',
      sourceItem: 'TruckTemperature',
      itemOwner: 'Data Engineering Team',
      workspace: 'RealTimeAnalytics',
      throughput: '850 msg/s',
      status: 'active',
    },
    {
      id: '3',
      name: 'userActivityStream',
      type: 'Kafka Stream',
      sourceItem: 'UserEvents',
      itemOwner: 'Analytics Team',
      workspace: 'UserAnalytics',
      throughput: '3.5k msg/s',
      status: 'active',
    },
    {
      id: '4',
      name: 'sensorDataStream',
      type: 'Kafka Stream',
      sourceItem: 'IoTSensors',
      itemOwner: 'IoT Team',
      workspace: 'IoTPlatform',
      throughput: '0 msg/s',
      status: 'idle',
    },
  ];

  const actionCards = [
    {
      icon: AlertTriangle,
      title: 'Detect anomalies',
      description: 'Create an anomaly detector to surface unexpected patterns in real-time data',
      linkText: 'Learn more',
    },
    {
      icon: Database,
      title: 'Subscribe to events',
      description: 'Convert events into streams for immediate transformation and routing',
      linkText: 'Learn more',
    },
    {
      icon: Activity,
      title: 'Act on Job events',
      description: 'Automate responses to status changes in Job events',
      linkText: 'Learn more',
    },
    {
      icon: Eye,
      title: 'Visualize data',
      description: 'Observe data in a real-time dashboard for fast, informed decisions',
      linkText: 'Learn more',
    },
  ];

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'text-green-600';
      case 'idle':
        return 'text-gray-400';
      case 'error':
        return 'text-red-600';
      default:
        return 'text-gray-600';
    }
  };

  return (
    <div className="h-full flex">
      {/* Left Sidebar */}
      <div className="w-64 bg-white border-r border-gray-200 flex flex-col">
        <div className="p-4">
          <div className="flex items-center gap-2 mb-6">
            <Zap className="w-6 h-6 text-cloudera-blue" />
            <h2 className="text-lg font-semibold">Real-Time hub</h2>
          </div>

          {/* Connect to */}
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-gray-700 mb-3">Connect to</h3>
            <button className="w-full flex items-center gap-2 px-3 py-2 text-sm bg-white border border-gray-300 rounded hover:bg-gray-50 mb-2">
              <Database className="w-4 h-4" />
              Data sources
            </button>
            <button className="w-full flex items-center gap-2 px-3 py-2 text-sm bg-white border border-gray-300 rounded hover:bg-gray-50">
              <Waves className="w-4 h-4" />
              Kafka sources
            </button>
          </div>

          {/* Subscribe to */}
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-3">Subscribe to</h3>
            <button className="w-full flex items-center gap-2 px-3 py-2 text-sm bg-white border border-gray-300 rounded hover:bg-gray-50 mb-2">
              <Zap className="w-4 h-4" />
              Fabric events
            </button>
            <button className="w-full flex items-center gap-2 px-3 py-2 text-sm bg-white border border-gray-300 rounded hover:bg-gray-50">
              <Activity className="w-4 h-4" />
              Kafka events
            </button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 overflow-auto">
        <div className="p-8 max-w-[1400px] mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-semibold mb-2">Discover, analyze, and act on data-in-motion</h1>
          </div>

          {/* Action Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            {actionCards.map((card, index) => (
              <div key={index} className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
                <div className="flex items-start justify-between mb-4">
                  <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-orange-500 to-pink-500 flex items-center justify-center">
                    <card.icon className="w-5 h-5 text-white" />
                  </div>
                  <TrendingUp className="w-5 h-5 text-gray-400" />
                </div>
                <h3 className="font-semibold mb-2">{card.title}</h3>
                <p className="text-sm text-gray-600 mb-3">{card.description}</p>
                <a href="#" className="text-sm text-cloudera-blue hover:underline">
                  {card.linkText} →
                </a>
              </div>
            ))}
          </div>

          {/* Recent Streaming Data */}
          <div className="bg-white rounded-lg border border-gray-200">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-semibold">Recent streaming data</h2>
              <button className="flex items-center gap-2 px-4 py-2 bg-cloudera-green text-white rounded hover:bg-green-600">
                <Plus className="w-4 h-4" />
                Add data
              </button>
            </div>

            {/* Filters */}
            <div className="flex items-center gap-4 px-6 py-4 bg-gray-50 border-b border-gray-200">
              <span className="text-sm font-medium">Filter by</span>
              <select
                value={dataTypeFilter}
                onChange={(e) => setDataTypeFilter(e.target.value)}
                className="px-3 py-1.5 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue"
              >
                <option value="all">All data types</option>
                <option value="kafka">Kafka Streams</option>
                <option value="flink">Flink Streams</option>
              </select>
              <button className="ml-auto flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-white">
                <Filter className="w-4 h-4" />
                More filters
              </button>
              <input
                type="text"
                placeholder="Search for streaming data"
                className="px-3 py-1.5 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue w-64"
              />
            </div>

            {/* Data Table */}
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Data
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Source Item
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Item Owner
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Workspace
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Throughput
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                      Status
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {streamingData.map((item) => (
                    <tr key={item.id} className="hover:bg-gray-50 cursor-pointer">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <Activity className="w-5 h-5 text-orange-500" />
                          <span className="text-sm font-medium">{item.name}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <Waves className="w-4 h-4 text-blue-500" />
                          <span className="text-sm text-gray-600">{item.sourceItem}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-gray-600">{item.itemOwner}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-gray-600">{item.workspace}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm font-medium">{item.throughput}</span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <div className={`w-2 h-2 rounded-full ${item.status === 'active' ? 'bg-green-500' : 'bg-gray-400'}`}></div>
                          <span className={`text-sm font-medium ${getStatusColor(item.status)}`}>
                            {item.status.charAt(0).toUpperCase() + item.status.slice(1)}
                          </span>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RealTimeHub;
