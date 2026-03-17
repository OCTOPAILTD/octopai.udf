import { useState, ReactNode } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Home,
  LayoutGrid,
  PanelLeftOpen,
  Monitor,
  Zap,
  Database,
  Workflow,
  User,
  Search,
  Settings,
  Bell,
  HelpCircle,
  Download,
  MoreVertical,
  Menu,
} from 'lucide-react';

interface LayoutProps {
  children: ReactNode;
}

interface NavItem {
  id: string;
  label: string;
  icon: any;
  path: string;
}

const Layout = ({ children }: LayoutProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [workspacesOpen, setWorkspacesOpen] = useState(false);
  const [newItemOpen, setNewItemOpen] = useState(false);

  const navItems: NavItem[] = [
    { id: 'home', label: 'Home', icon: Home, path: '/home' },
    { id: 'workspaces', label: 'Workspaces', icon: LayoutGrid, path: '/workspaces' },
    { id: 'udf-catalog', label: 'UDF Catalog', icon: Database, path: '/udf-catalog' },
    { id: 'monitor', label: 'Monitor', icon: Monitor, path: '/monitor' },
    { id: 'realtime', label: 'Real-Time', icon: Zap, path: '/realtime' },
    { id: 'workloads', label: 'Workloads', icon: Workflow, path: '/workloads' },
  ];

  const isActive = (path: string) => location.pathname === path;

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Left Sidebar */}
      <div className="w-12 bg-white border-r border-gray-200 flex flex-col items-center py-3">
        <div className="mb-6">
          <Menu className="w-5 h-5 text-gray-700" />
        </div>

        {navItems.map((item) => (
          <button
            key={item.id}
            onClick={() => navigate(item.path)}
            className={`w-10 h-10 mb-1 flex items-center justify-center rounded hover:bg-gray-100 transition-colors relative group ${
              isActive(item.path) ? 'bg-blue-50 text-cloudera-blue' : 'text-gray-600'
            }`}
            title={item.label}
          >
            <item.icon className="w-5 h-5" />
            {isActive(item.path) && (
              <div className="absolute left-0 w-0.5 h-8 bg-cloudera-blue rounded-r" />
            )}
            <div className="absolute left-12 bg-gray-900 text-white text-xs px-2 py-1 rounded opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none whitespace-nowrap z-50">
              {item.label}
            </div>
          </button>
        ))}

        <div className="mt-auto mb-2">
          <button className="w-10 h-10 mb-1 flex items-center justify-center rounded hover:bg-gray-100 transition-colors text-gray-600">
            <User className="w-5 h-5" />
          </button>
          <button className="w-10 h-10 flex items-center justify-center rounded hover:bg-gray-100 transition-colors text-gray-600">
            <MoreVertical className="w-5 h-5" />
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col">
        {/* Top Navigation Bar */}
        <div className="h-12 bg-white border-b border-gray-200 flex items-center px-4">
          <div className="text-sm font-semibold text-cloudera-blue mr-6">
            Cloudera Fabric Studio
          </div>

          {/* Search Bar */}
          <div className="flex-1 max-w-md">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder="Search"
                className="w-full pl-10 pr-4 py-1.5 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-cloudera-blue focus:border-transparent"
              />
            </div>
          </div>

          {/* Right Icons */}
          <div className="flex items-center gap-2 ml-auto">
            <div className="text-xs text-gray-600 mr-2">
              <div className="font-medium">Trials activated:</div>
              <div>26 days left</div>
            </div>
            <button className="p-2 hover:bg-gray-100 rounded">
              <PanelLeftOpen className="w-4 h-4 text-gray-600" />
            </button>
            <button className="p-2 hover:bg-gray-100 rounded relative">
              <Bell className="w-4 h-4 text-gray-600" />
              <span className="absolute top-1 right-1 w-2 h-2 bg-green-500 rounded-full"></span>
            </button>
            <button className="p-2 hover:bg-gray-100 rounded">
              <Settings className="w-4 h-4 text-gray-600" />
            </button>
            <button className="p-2 hover:bg-gray-100 rounded">
              <Download className="w-4 h-4 text-gray-600" />
            </button>
            <button className="p-2 hover:bg-gray-100 rounded">
              <HelpCircle className="w-4 h-4 text-gray-600" />
            </button>
            <button className="p-2 hover:bg-gray-100 rounded">
              <User className="w-5 h-5 text-gray-600" />
            </button>
          </div>
        </div>

        {/* Page Content */}
        <div className="flex-1 overflow-auto">
          {children}
        </div>
      </div>
    </div>
  );
};

export default Layout;

