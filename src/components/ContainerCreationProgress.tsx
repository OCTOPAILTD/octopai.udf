import { useEffect, useState, useRef } from 'react';
import { CheckCircle, XCircle, Loader2, GripVertical } from 'lucide-react';
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

interface ContainerCreationProgressProps {
  isOpen: boolean;
  containerName: string;
  error?: string | null;
  onClose: (status: 'success' | 'error') => void;
  containerId?: string | null;
}

interface LogEntry {
  timestamp: string;
  message: string;
  type: 'info' | 'success' | 'error';
}

const ContainerCreationProgress = ({ isOpen, containerName, error, onClose, containerId }: ContainerCreationProgressProps) => {
  const [progress, setProgress] = useState(0);
  const [currentStep, setCurrentStep] = useState('Initializing...');
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [status, setStatus] = useState<'creating' | 'success' | 'error'>('creating');
  
  // Drag state
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const modalRef = useRef<HTMLDivElement>(null);

  // Handle error prop
  useEffect(() => {
    if (error) {
      setStatus('error');
      setCurrentStep('Error occurred');
      addLog(`❌ ${error}`, 'error');
    }
  }, [error]);

  // Drag handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget || (e.target as HTMLElement).closest('.drag-handle')) {
      setIsDragging(true);
      setDragStart({
        x: e.clientX - position.x,
        y: e.clientY - position.y
      });
    }
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (isDragging) {
      setPosition({
        x: e.clientX - dragStart.x,
        y: e.clientY - dragStart.y
      });
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  // Add/remove mouse event listeners for dragging
  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, dragStart]);

  // Reset position when modal opens
  useEffect(() => {
    if (isOpen) {
      setPosition({ x: 0, y: 0 });
    }
  }, [isOpen]);

  const addLog = (message: string, type: 'info' | 'success' | 'error' = 'info') => {
    setLogs(prev => [...prev, {
      timestamp: new Date().toLocaleTimeString(),
      message,
      type
    }]);
  };

  useEffect(() => {
    if (!isOpen) {
      // Reset state when modal closes
      setProgress(0);
      setCurrentStep('Initializing...');
      setLogs([]);
      setStatus('creating');
      return;
    }

    if (!containerId) {
      // Show user-friendly messages while container is being created
      setProgress(10);
      setCurrentStep(`Creating ${containerName} container...`);
      addLog('Initializing Docker container...', 'info');
      addLog('Pulling Docker image (this may take 2-5 minutes for large images)...', 'info');
      addLog('Please wait, this is normal for first-time image downloads...', 'info');
      
      // Poll for container ID with timeout
      const pollInterval = setInterval(() => {
        if (containerId) {
          clearInterval(pollInterval);
        }
      }, 500);
      
      let timeoutCount = 0;
      const timeoutMessages = [
        { delay: 10000, message: 'Still downloading image...' },
        { delay: 30000, message: 'Large images can take several minutes to download...' },
        { delay: 60000, message: 'Image download in progress. This is normal for first-time pulls...' },
        { delay: 90000, message: 'Almost there! Image download is completing...' },
      ];
      
      const timeouts = timeoutMessages.map(({ delay, message }) => 
        setTimeout(() => {
          addLog(message, 'info');
          setProgress(Math.min(10 + (delay / 1000) * 0.5, 30)); // Gradually increase progress
        }, delay)
      );
      
      return () => {
        clearInterval(pollInterval);
        timeouts.forEach(clearTimeout);
      };
    }

    let eventSource: EventSource | null = null;
    let timeoutId: number;

    const startStreamingLogs = () => {
      addLog(`Starting container ${containerName}...`, 'info');
      setProgress(10);
      setCurrentStep('Container created, waiting for startup...');

      eventSource = new EventSource(`${getApiUrl()}/api/containers/${containerId}/logs/stream`);

      eventSource.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          
          if (data.message) {
            addLog(data.message, data.type || 'info');
          }

          if (data.progress) {
            setProgress(data.progress);
          }
          
          if (data.step) {
            setCurrentStep(data.step);
          }
          
          if (data.status === 'success') {
            setStatus('success');
            setProgress(100);
            setCurrentStep('Container is ready!');
            const nameLower = containerName.toLowerCase();
            let readyMessage = 'Container is ready to use!';
            if (nameLower.includes('nifi')) {
              readyMessage = '✅ NiFi is ready to use!';
            } else if (nameLower.includes('trino')) {
              readyMessage = '✅ Trino is ready to use!';
            } else if (nameLower.includes('impala')) {
              readyMessage = '✅ Impala is ready to use!';
            } else if (nameLower.includes('hbase')) {
              readyMessage = '✅ HBase is ready to use!';
            } else if (nameLower.includes('hive')) {
              readyMessage = '✅ Hive is ready to use!';
            }
            addLog(readyMessage, 'success');
            eventSource?.close();
            clearTimeout(timeoutId);
          } else if (data.status === 'error') {
            setStatus('error');
            setCurrentStep(`Error: ${data.message}`);
            addLog(`❌ ${data.message}`, 'error');
            eventSource?.close();
            clearTimeout(timeoutId);
          }
        } catch (err) {
          console.error('Error parsing SSE data:', err);
        }
      };

      eventSource.onerror = (err) => {
        console.error('EventSource failed:', err);
        addLog('Connection to server lost or stream ended', 'error');
        setStatus('error');
        eventSource?.close();
        clearTimeout(timeoutId);
      };

      // Set a timeout for the entire creation process (5 minutes)
      timeoutId = setTimeout(() => {
        addLog('Container creation timed out. Container may still be starting...', 'error');
        setStatus('error');
        eventSource?.close();
      }, 300000); // 5 minutes
    };

    // Start streaming immediately
    startStreamingLogs();

    return () => {
      eventSource?.close();
      clearTimeout(timeoutId);
    };
  }, [isOpen, containerId, containerName]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div 
        ref={modalRef}
        className="bg-white rounded-lg shadow-xl w-full max-w-2xl mx-4 relative"
        style={{
          transform: `translate(${position.x}px, ${position.y}px)`,
          cursor: isDragging ? 'grabbing' : 'default',
          transition: isDragging ? 'none' : 'transform 0.1s ease-out'
        }}
      >
        {/* Header - Draggable */}
        <div 
          className="px-6 py-4 border-b border-gray-200 drag-handle flex items-center gap-2 cursor-move select-none"
          onMouseDown={handleMouseDown}
        >
          <GripVertical className="w-5 h-5 text-gray-400" />
          <h2 className="text-xl font-semibold text-gray-900 flex-1">
            Creating Container: {containerName}
          </h2>
        </div>

        {/* Progress Bar */}
        <div className="px-6 py-4">
          <div className="flex items-center gap-3 mb-2">
            {status === 'creating' && (
              <Loader2 className="w-5 h-5 text-cloudera-blue animate-spin" />
            )}
            {status === 'success' && (
              <CheckCircle className="w-5 h-5 text-green-500" />
            )}
            {status === 'error' && (
              <XCircle className="w-5 h-5 text-red-500" />
            )}
            <span className="text-sm font-medium text-gray-700">{currentStep}</span>
          </div>
          
          <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
            <div 
              className="bg-cloudera-blue h-full transition-all duration-500 ease-out"
              style={{ width: `${progress}%` }}
            />
          </div>
          
          <div className="text-right mt-1">
            <span className="text-sm text-gray-600">{progress}%</span>
          </div>
        </div>

        {/* Logs */}
        <div className="px-6 pb-4">
          <div className="bg-gray-900 text-gray-100 rounded-lg p-4 h-64 overflow-y-auto font-mono text-sm">
            {logs.map((log, index) => (
              <div key={index} className="mb-1">
                <span className="text-gray-500">[{log.timestamp}]</span>{' '}
                <span className={
                  log.type === 'error' ? 'text-red-400' :
                  log.type === 'success' ? 'text-green-400' :
                  'text-gray-300'
                }>
                  {log.message}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex justify-end">
          {status === 'success' && (
            <button
              onClick={() => onClose('success')}
              className="px-4 py-2 bg-cloudera-blue text-white rounded-lg hover:bg-cloudera-blue-dark transition-colors"
            >
              Continue
            </button>
          )}
          {status === 'error' && (
            <button
              onClick={() => onClose('error')}
              className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition-colors"
            >
              Close
            </button>
          )}
          {status === 'creating' && (
            <button
              disabled
              className="px-4 py-2 bg-gray-300 text-gray-500 rounded-lg cursor-not-allowed"
            >
              Please wait...
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default ContainerCreationProgress;
