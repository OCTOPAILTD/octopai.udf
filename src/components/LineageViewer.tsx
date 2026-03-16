import { useState, useEffect } from 'react';
import ReactFlow, {
  Node,
  Edge,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  MarkerType,
  Position,
  Handle,
} from 'reactflow';
import { GitGraph, Database, Loader2, RefreshCw, AlertCircle, Layers } from 'lucide-react';
import 'reactflow/dist/style.css';
import config from '../config';

interface LineageViewerProps {
  assetUrn: string;
  assetName: string;
}

interface LineageData {
  nodes: Node[];
  edges: Edge[];
}

// Custom node component with handles for each column
const ColumnNode = ({ data }: any) => {
  const { label, columns, isCenter, isFile, urn } = data;
  
  const handleClick = () => {
    if (urn) {
      const encodedUrn = encodeURIComponent(urn);
      window.location.href = `/udf-catalog/entity/${encodedUrn}`;
    }
  };
  
  return (
    <div 
      className="px-3 py-2 w-[240px] max-w-[240px] cursor-pointer hover:shadow-lg transition-shadow" 
      onClick={handleClick}
      title={urn ? "Click to view details" : ""}
    >
      <div className="flex items-center gap-2 mb-2 pb-2 border-b">
        {isFile ? (
          <svg className="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
        ) : (
          <Database className="w-5 h-5 text-blue-600" />
        )}
        <div>
          <div className="font-semibold text-sm">{label}</div>
          <div className="text-xs text-gray-500">{data.platform || 'unknown'}</div>
        </div>
      </div>
      {columns && columns.length > 0 ? (
        <div className="mt-2 overflow-hidden">
          <div className="text-xs font-semibold text-gray-600 mb-1">Columns ({columns.length}):</div>
          <div className="space-y-1 max-h-[160px] overflow-y-auto overflow-x-hidden pr-1" style={{ scrollbarWidth: 'thin' }}>
            {columns.map((col: string, idx: number) => (
              <div key={idx} className="relative text-xs bg-blue-50 px-2 py-1.5 rounded flex items-center gap-1 border border-blue-200 min-w-0">
                {/* Left handle for incoming connections */}
                <Handle
                  type="target"
                  position={Position.Left}
                  id={`${col}-target`}
                  style={{
                    left: -8,
                    width: 8,
                    height: 8,
                    background: '#3b82f6',
                    border: '2px solid white',
                    borderRadius: '50%',
                  }}
                />
                <span className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0"></span>
                <span className="truncate text-gray-700 font-medium block overflow-hidden text-ellipsis whitespace-nowrap">{col}</span>
                {/* Right handle for outgoing connections */}
                <Handle
                  type="source"
                  position={Position.Right}
                  id={`${col}-source`}
                  style={{
                    right: -8,
                    width: 8,
                    height: 8,
                    background: '#3b82f6',
                    border: '2px solid white',
                    borderRadius: '50%',
                  }}
                />
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="mt-2 text-xs text-gray-400 italic">
          No schema available
        </div>
      )}
    </div>
  );
};

// Custom node component for dataset-level view
const DatasetNode = ({ data }: any) => {
  const { label, platform, isCenter, isFile, urn } = data;
  
  const handleClick = () => {
    if (urn) {
      const encodedUrn = encodeURIComponent(urn);
      window.location.href = `/udf-catalog/entity/${encodedUrn}`;
    }
  };
  
  return (
    <div 
      className="px-3 py-2 cursor-pointer hover:bg-gray-50 transition-colors rounded" 
      onClick={handleClick}
      title="Click to view details"
      style={{
        background: isCenter ? '#dbeafe' : (isFile ? '#fef3c7' : 'white'),
        border: isCenter ? '2px solid #3b82f6' : (isFile ? '2px solid #f59e0b' : '1px solid #e5e7eb'),
        borderRadius: '8px',
        padding: '8px',
        minWidth: '160px',
      }}
    >
      {/* Connection handles for edges */}
      <Handle
        type="target"
        position={Position.Left}
        style={{ background: '#3b82f6', width: '8px', height: '8px' }}
      />
      <Handle
        type="source"
        position={Position.Right}
        style={{ background: '#3b82f6', width: '8px', height: '8px' }}
      />
      
      <div className="flex items-center gap-2 mb-1">
        {isFile ? (
          <svg className="w-4 h-4 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
        ) : (
          <Database className="w-4 h-4 text-blue-600" />
        )}
        <span className="font-semibold text-sm">{label || 'Unknown'}</span>
      </div>
      {platform && (
        <div className="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
          {platform}
        </div>
      )}
    </div>
  );
};

// Register the custom node types
const nodeTypes = {
  columnNode: ColumnNode,
  datasetNode: DatasetNode,
};

export const LineageViewer = ({ assetUrn, assetName }: LineageViewerProps) => {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [loading, setLoading] = useState(true);
  const [loadingMessage, setLoadingMessage] = useState('Loading lineage...');
  const [error, setError] = useState<string>('');
  const [showColumns, setShowColumns] = useState(false);
  const [columnLineage, setColumnLineage] = useState<any[]>([]);

  useEffect(() => {
    fetchLineage();
  }, [assetUrn, showColumns]);

  const fetchLineage = async () => {
    try {
      setLoading(true);
      setError('');
      setLoadingMessage('Loading lineage...');

      // Single fetch — backend already does full BFS traversal (both directions, all hops)
      const url = `${config.backendUrl}/api/atlas/lineage/${encodeURIComponent(assetUrn)}?direction=BOTH&depth=10`;
      const response = await fetch(url);

      if (!response.ok) throw new Error('Failed to fetch lineage');

      const lineageData = await response.json();
      console.log(`[LineageViewer] Lineage: ${lineageData.nodes?.length} nodes, ${lineageData.edges?.length} edges`);

      const combinedLineageData = {
        nodes: lineageData.nodes || [],
        edges: lineageData.edges || [],
        columnLineage: lineageData.columnLineage,
        nodeSchemas: new Map<string, any>(),
      };
      
      if (showColumns) {
        // Column-level view - show columns with field-to-field arrows
        console.log('[LineageViewer] Building COLUMN-level view with field-to-field arrows');
        
        setColumnLineage(combinedLineageData.columnLineage || []);
        const colNodes = buildColumnLevelNodes(combinedLineageData);
        const colEdges = buildColumnLevelEdges(combinedLineageData);
        
        console.log('[LineageViewer] Built nodes:', colNodes.length, 'edges:', colEdges.length);
        
        setNodes(colNodes);
        setEdges(colEdges);
      } else {
        // Dataset-level view - show ONLY DATASET nodes, skip DATA_JOB nodes
        console.log('[LineageViewer] Building DATASET-level view with DATASET nodes only');
        
        // Include all nodes (processors, tables, etc.) - exclude DATA_JOB duplicates only
        const datasetNodes = combinedLineageData.nodes.filter((node: any) => node.type !== 'DATA_JOB');
        console.log(`[LineageViewer] Showing ${datasetNodes.length} nodes (from ${combinedLineageData.nodes.length} total)`);

        // Build a safe node ID map: URN → safe alphanumeric ID
        // React Flow has issues with '/', ':', '%' in node IDs
        const urnToSafeId = new Map<string, string>();
        datasetNodes.forEach((node: any, index: number) => {
          urnToSafeId.set(node.urn, `node-${index}`);
        });

        // Topological sort: assign each node a column position based on edge depth
        const edges = combinedLineageData.edges || [];
        const nodeDepth = new Map<string, number>();

        // BFS from nodes with no incoming edges
        const inDegree = new Map<string, number>();
        datasetNodes.forEach((n: any) => inDegree.set(n.urn, 0));
        edges.forEach((e: any) => {
          if (inDegree.has(e.target)) inDegree.set(e.target, (inDegree.get(e.target) || 0) + 1);
        });

        const queue: string[] = [];
        datasetNodes.forEach((n: any) => {
          if ((inDegree.get(n.urn) || 0) === 0) {
            queue.push(n.urn);
            nodeDepth.set(n.urn, 0);
          }
        });

        while (queue.length > 0) {
          const cur = queue.shift()!;
          const curDepth = nodeDepth.get(cur) || 0;
          edges.forEach((e: any) => {
            if (e.source === cur && inDegree.has(e.target)) {
              const newDepth = curDepth + 1;
              if (!nodeDepth.has(e.target) || nodeDepth.get(e.target)! < newDepth) {
                nodeDepth.set(e.target, newDepth);
              }
              queue.push(e.target);
            }
          });
        }

        // Assign any unvisited nodes a depth based on center
        datasetNodes.forEach((n: any) => {
          if (!nodeDepth.has(n.urn)) nodeDepth.set(n.urn, 0);
        });

        // Group nodes by depth column, stack vertically within each column
        const depthGroups = new Map<number, any[]>();
        datasetNodes.forEach((n: any) => {
          const d = nodeDepth.get(n.urn) || 0;
          if (!depthGroups.has(d)) depthGroups.set(d, []);
          depthGroups.get(d)!.push(n);
        });

        const flowNodes: Node[] = datasetNodes.map((node: any) => {
          const isCenter = node.urn === assetUrn;
          const isFile = node.platform === 'file';
          const safeId = urnToSafeId.get(node.urn) || `node-${node.urn}`;
          const depth = nodeDepth.get(node.urn) || 0;
          const group = depthGroups.get(depth) || [];
          const rowIdx = group.indexOf(node);
          const rowCount = group.length;

          return {
            id: safeId,
            type: 'datasetNode',
            position: {
              x: depth * 300,
              y: rowIdx * 120 - ((rowCount - 1) * 60),
            },
            data: {
              label: node.name || 'Unknown',
              platform: node.platform,
              isCenter,
              isFile,
              urn: node.urn,
            },
          };
        });
        
        const flowEdges: Edge[] = (combinedLineageData.edges || [])
          .filter((edge: any) => urnToSafeId.has(edge.source) && urnToSafeId.has(edge.target))
          .map((edge: any, index: number) => ({
            id: `edge-${index}`,
            source: urnToSafeId.get(edge.source)!,
            target: urnToSafeId.get(edge.target)!,
            type: 'smoothstep',
            animated: true,
            markerEnd: { type: MarkerType.ArrowClosed },
            style: { stroke: '#6b7280', strokeWidth: 2 },
          }));
        
        console.log('[LineageViewer] Built nodes:', flowNodes.length, 'edges:', flowEdges.length);
        
        setNodes(flowNodes);
        setEdges(flowEdges);
      }
      
      setLoading(false);
    } catch (err: any) {
      console.error('Failed to fetch lineage:', err);
      setError(err.message || 'Failed to load lineage data');
      setLoading(false);
    }
  };

  // Build a safe React Flow node ID from a URN (avoid '/', ':', '%' which break React Flow)
  const urnToNodeId = (urn: string): string =>
    'n-' + urn.replace(/[^a-zA-Z0-9-]/g, '_');

  const buildColumnLevelNodes = (lineageData: any): Node[] => {
    const nodes: Node[] = [];
    const datasets = new Map<string, any>();
    
    console.log('[LineageViewer] buildColumnLevelNodes - Total nodes:', lineageData.nodes.length);
    
    // Group all nodes (processors, tables, etc.) - exclude DATA_JOB duplicates
    lineageData.nodes.forEach((node: any) => {
      if (node.type !== 'DATA_JOB') {
        datasets.set(node.urn, node);
      }
    });
    
    console.log('[LineageViewer] Total datasets to render:', datasets.size);
    
    // Create dataset nodes with column information
    let xPos = 0;
    datasets.forEach((dataset, urn) => {
      const isCenter = urn === assetUrn;
      const isFile = dataset.platform === 'file';
      
      // Get columns for this dataset from column lineage mappings
      const columns = new Set<string>();
      
      if (dataset.columns && Array.isArray(dataset.columns)) {
        dataset.columns.forEach((col: string) => columns.add(col));
      }
      
      if (columns.size === 0) {
        const relatedColumns = lineageData.columnLineage?.filter((col: any) => 
          col.source_dataset === urn || col.target_dataset === urn
        ) || [];
        relatedColumns.forEach((col: any) => {
          if (col.source_dataset === urn && col.source_field) columns.add(col.source_field);
          if (col.target_dataset === urn && col.target_field) columns.add(col.target_field);
        });
      }
      
      if (lineageData.nodeSchemas?.has(urn)) {
        const nodeSchema = lineageData.nodeSchemas.get(urn);
        nodeSchema.forEach((field: any) => {
          if (field.fieldPath) columns.add(field.fieldPath);
        });
      }
      
      if (isCenter && lineageData.schema?.fields && columns.size === 0) {
        lineageData.schema.fields.forEach((field: any) => {
          if (field.fieldPath) columns.add(field.fieldPath);
        });
      }
      
      console.log(`[LineageViewer] Dataset ${dataset.name}: ${columns.size} columns, isCenter: ${isCenter}`);
      
      nodes.push({
        id: urnToNodeId(urn),   // Safe ID - no special chars
        type: 'columnNode',
        position: { x: xPos, y: 150 },
        data: {
          label: dataset.name,
          platform: dataset.platform || 'unknown',
          columns: Array.from(columns),
          isCenter,
          isFile,
          urn,
        },
        style: {
          background: isCenter ? '#dbeafe' : (isFile ? '#fef3c7' : 'white'),
          border: isCenter ? '3px solid #3b82f6' : (isFile ? '2px solid #f59e0b' : '2px solid #e5e7eb'),
          borderRadius: '8px',
          padding: '8px',
          width: '256px',
          maxWidth: '256px',
          overflow: 'hidden',
        },
      });
      
      xPos += 350;
    });
    
    return nodes;
  };

  const buildColumnLevelEdges = (lineageData: any): Edge[] => {
    const edges: Edge[] = [];
    
    // DO NOT add dataset-level edges in column-level view - only show column-to-column arrows
    
    // Add column-level edges (connecting specific fields)
    if (lineageData.columnLineage && lineageData.columnLineage.length > 0) {
      console.log(`[LineageViewer] Rendering ${lineageData.columnLineage.length} field-to-field edges`);
      console.log('[LineageViewer] Column lineage data:', lineageData.columnLineage);
      
      lineageData.columnLineage.forEach((col: any, index: number) => {
        console.log(`[LineageViewer] Processing lineage ${index}:`, col);
        
        if (col.source_field && col.source_field !== 'N/A' && col.target_field && col.target_field !== 'N/A') {
          // Create arrow from specific source field handle to target field handle
          const colors = ['#3b82f6', '#2563eb', '#1d4ed8', '#1e40af'];
          const color = colors[index % colors.length];
          
          const edge = {
            id: `field-edge-${index}-${col.source_field}-to-${col.target_field}`,
            source: urnToNodeId(col.source_dataset),
            sourceHandle: `${col.source_field}-source`,
            target: urnToNodeId(col.target_dataset),
            targetHandle: `${col.target_field}-target`,
            type: 'smoothstep',
            animated: true,
            style: { 
              stroke: color, 
              strokeWidth: 2,
            },
            markerEnd: { 
              type: MarkerType.ArrowClosed, 
              color: color,
              width: 12,
              height: 12
            },
          };
          
          console.log(`[LineageViewer] Created edge:`, edge);
          edges.push(edge);
        } else {
          console.log(`[LineageViewer] Skipped lineage ${index} - missing fields:`, {
            source_field: col.source_field,
            target_field: col.target_field
          });
        }
      });
      
      console.log(`[LineageViewer] Created ${edges.length} field-to-field edges`);
    } else {
      console.log('[LineageViewer] No column lineage data available');
    }
    
    return edges;
  };

  return (
    <div className="relative w-full h-[600px] bg-gray-50 rounded-lg border border-gray-200">
      {/* Toggle Button */}
      <div className="absolute top-4 right-4 z-10 bg-white rounded-lg shadow-lg border border-gray-200">
        <button
          onClick={() => setShowColumns(!showColumns)}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
            showColumns 
              ? 'bg-blue-600 text-white' 
              : 'bg-white text-gray-700 hover:bg-gray-50'
          }`}
        >
          <Layers className="w-4 h-4" />
          <span className="text-sm font-medium">
            {showColumns ? 'Column-Level' : 'Dataset-Level'}
          </span>
        </button>
      </div>

      {loading && (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <Loader2 className="w-8 h-8 animate-spin text-blue-600 mx-auto mb-2" />
            <p className="text-gray-600">{loadingMessage}</p>
            <p className="text-gray-400 text-sm mt-1">This may take a few seconds...</p>
          </div>
        </div>
      )}

      {error && (
        <div className="flex items-center justify-center h-full">
          <div className="text-center max-w-md">
            <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Unable to Load Lineage</h3>
            <p className="text-gray-600 mb-4">{error}</p>
            <button
              onClick={fetchLineage}
              className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
            >
              <RefreshCw className="w-4 h-4" />
              Retry
            </button>
          </div>
        </div>
      )}

      {!loading && !error && nodes.length === 0 && (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <GitGraph className="w-12 h-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-600">No lineage data available for this entity</p>
          </div>
        </div>
      )}

      {!loading && !error && nodes.length > 0 && (
        <>
          {showColumns && columnLineage.length > 0 && (
            <div className="absolute top-16 right-4 z-10 bg-blue-50 border border-blue-200 rounded-lg px-3 py-2 text-sm text-blue-800">
              <div className="font-semibold">Column Mappings: {columnLineage.length}</div>
            </div>
          )}
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            nodeTypes={nodeTypes}
            fitView
            attributionPosition="bottom-left"
          >
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </>
      )}
    </div>
  );
};
