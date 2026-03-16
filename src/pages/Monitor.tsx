import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Activity,
  CheckCircle,
  Clock,
  TrendingUp,
  TrendingDown,
  BarChart3,
  Calendar,
  ExternalLink,
  Trash2,
  StopCircle,
} from 'lucide-react';
import { containerService, type Container } from '../services/containerService';

interface MetricCard {
  label: string;
  value: string;
  change: string;
  trend: 'up' | 'down';
  icon: any;
}


const Monitor = () => {
  const [timeRange, setTimeRange] = useState('24h');
  const [containers, setContainers] = useState<Container[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Fetch containers
  useEffect(() => {
    loadContainers();
    const interval = setInterval(loadContainers, 5000); // Refresh every 5 seconds
    return () => clearInterval(interval);
  }, []);

  const loadContainers = async () => {
    try {
      const data = await containerService.listContainers();
      setContainers(data);
      setLoading(false);
    } catch (error) {
      console.error('Failed to load containers:', error);
      setLoading(false);
    }
  };

  const handleOpenContainer = (container: Container) => {
    if (container.name.includes('nifi')) {
      navigate(`/tool/nifi/${container.id}`);
    } else if (container.name.includes('kafka')) {
      navigate(`/tool/kafka/${container.id}`);
    }
  };

  const handleStopContainer = async (id: string) => {
    if (confirm('Are you sure you want to stop this container?')) {
      try {
        await containerService.stopContainer(id);
        loadContainers();
      } catch (error) {
        console.error('Failed to stop container:', error);
        alert('Failed to stop container');
      }
    }
  };

  const handleRemoveContainer = async (id: string) => {
    if (confirm('Are you sure you want to remove this container? This action cannot be undone.')) {
      try {
        await containerService.removeContainer(id);
        loadContainers();
      } catch (error) {
        console.error('Failed to remove container:', error);
        alert('Failed to remove container');
      }
    }
  };

  const runningContainers = containers.filter(c => c.status === 'running' && !c.name.includes('cloudera-fabric-studio'));
  const stoppedContainers = containers.filter(c => c.status !== 'running' && !c.name.includes('cloudera-fabric-studio'));
  
  const metrics: MetricCard[] = [
    {
      label: 'Running Containers',
      value: runningContainers.length.toString(),
      change: '+' + runningContainers.length,
      trend: 'up',
      icon: Activity,
    },
    {
      label: 'Total Containers',
      value: (runningContainers.length + stoppedContainers.length).toString(),
      change: 'Active',
      trend: 'up',
      icon: BarChart3,
    },
    {
      label: 'NiFi Instances',
      value: containers.filter(c => c.name.includes('nifi')).length.toString(),
      change: 'Running',
      trend: 'up',
      icon: CheckCircle,
    },
    {
      label: 'Kafka Instances',
      value: containers.filter(c => c.name.includes('kafka')).length.toString(),
      change: 'Running',
      trend: 'up',
      icon: Clock,
    },
  ];


  return (
    <div className="p-8 max-w-[1600px] mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-semibold">Monitor</h1>
        <div className="flex items-center gap-3">
          <select
            value={timeRange}
            onChange={(e) => setTimeRange(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue"
          >
            <option value="1h">Last hour</option>
            <option value="24h">Last 24 hours</option>
            <option value="7d">Last 7 days</option>
            <option value="30d">Last 30 days</option>
          </select>
          <button className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded hover:bg-gray-50">
            <Calendar className="w-4 h-4" />
            Custom range
          </button>
          <button className="px-4 py-2 bg-cloudera-blue text-white rounded hover:bg-blue-700">
            Refresh
          </button>
        </div>
      </div>

      {/* Metrics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {metrics.map((metric, index) => (
          <div key={index} className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="w-12 h-12 rounded-lg bg-blue-50 flex items-center justify-center">
                <metric.icon className="w-6 h-6 text-cloudera-blue" />
              </div>
              {metric.trend === 'up' ? (
                <TrendingUp className="w-5 h-5 text-green-500" />
              ) : (
                <TrendingDown className="w-5 h-5 text-green-500" />
              )}
            </div>
            <div className="text-2xl font-bold mb-1">{metric.value}</div>
            <div className="text-sm text-gray-600 mb-1">{metric.label}</div>
            <div className="text-sm text-green-600">{metric.change} vs. previous period</div>
          </div>
        ))}
      </div>

      {/* Containers Overview */}
      <div className="bg-white rounded-lg border border-gray-200 mb-8">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold">Running Containers</h2>
          <div className="flex items-center gap-3">
            <button 
              onClick={loadContainers}
              className="px-4 py-2 bg-cloudera-blue text-white rounded hover:bg-blue-700"
            >
              Refresh
            </button>
          </div>
        </div>

        {/* Status Summary */}
        <div className="flex items-center gap-8 px-6 py-4 bg-gray-50 border-b border-gray-200">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-green-500"></div>
            <span className="text-sm font-medium">Running: {runningContainers.length}</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-gray-500"></div>
            <span className="text-sm font-medium">Stopped: {stoppedContainers.length}</span>
          </div>
        </div>

        {/* Containers Table */}
        <div className="overflow-x-auto">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <div className="text-gray-500">Loading containers...</div>
            </div>
          ) : runningContainers.length === 0 ? (
            <div className="flex items-center justify-center py-12">
              <div className="text-center text-gray-500">
                <Activity className="w-12 h-12 mx-auto mb-4 text-gray-400" />
                <p>No containers running</p>
                <p className="text-sm">Create a new NiFi Flow or Kafka Topic to get started</p>
              </div>
            </div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                    Container Name
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                    Type
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-600 uppercase tracking-wider">
                    Port
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
                {runningContainers.map((container) => (
                  <tr key={container.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="text-sm font-medium">{container.name}</div>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-gray-600">
                        {container.image.includes('nifi') ? 'Apache NiFi' : 
                         container.image.includes('kafka') ? 'Apache Kafka' : 
                         container.image.includes('datahub') ? 'DataHub Metadata' :
                         container.image}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium ${
                          container.status === 'running' 
                            ? 'bg-green-100 text-green-700' 
                            : 'bg-gray-100 text-gray-700'
                        }`}
                      >
                        {container.status === 'running' ? (
                          <CheckCircle className="w-4 h-4" />
                        ) : (
                          <Clock className="w-4 h-4" />
                        )}
                        {container.status.charAt(0).toUpperCase() + container.status.slice(1)}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-gray-600">
                        {container.port || 'N/A'}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-xs text-gray-500 font-mono">
                        {container.id.substring(0, 12)}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        {/* Open in NiFi button */}
                        <button
                          onClick={() => handleOpenContainer(container)}
                          className="px-3 py-1.5 text-xs font-medium text-white bg-blue-600 hover:bg-blue-700 rounded flex items-center gap-1"
                          title="Open in NiFi"
                        >
                          <ExternalLink className="w-3 h-3" />
                          Open in NiFi
                        </button>
                        {/* Open in Octopai (DataHub) button */}
                        <button
                          onClick={() => window.open(`http://localhost:9002/dataset/urn:li:dataset:(urn:li:dataPlatform:nifi,${container.name},PROD)`, '_blank')}
                          className="px-3 py-1.5 text-xs font-medium text-white bg-green-600 hover:bg-green-700 rounded flex items-center gap-1"
                          title="Open in Octopai (DataHub)"
                        >
                          <ExternalLink className="w-3 h-3" />
                          Open in Octopai
                        </button>
                        <button
                          onClick={() => handleStopContainer(container.id)}
                          className="p-1.5 text-yellow-600 hover:bg-yellow-50 rounded"
                          title="Stop"
                        >
                          <StopCircle className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleRemoveContainer(container.id)}
                          className="p-1.5 text-red-600 hover:bg-red-50 rounded"
                          title="Remove"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Performance Chart Placeholder */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-xl font-semibold mb-4">Performance Trends</h2>
        <div className="h-64 flex items-center justify-center bg-gray-50 rounded-lg">
          <div className="text-center text-gray-500">
            <BarChart3 className="w-16 h-16 mx-auto mb-4 text-gray-400" />
            <p>Performance chart visualization</p>
            <p className="text-sm">(Chart library integration required)</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Monitor;
