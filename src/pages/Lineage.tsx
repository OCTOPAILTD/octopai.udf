import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { GitGraph, Loader2, AlertCircle, RefreshCw } from 'lucide-react';
import ReactFlow, {
  Node,
  Edge,
  Controls,
  Background,
  MiniMap,
  useNodesState,
  useEdgesState,
  MarkerType,
} from 'reactflow';
import 'reactflow/dist/style.css';

interface LineageNode {
  urn: string;
  name: string;
  type: string;
  platform: string;
}

interface LineageEdge {
  source: string;
  target: string;
  type: string;
  fineGrainedLineages?: {
    upstreamColumns: string[];
    downstreamColumn: string;
    transformType: string;
  }[];
}

const Lineage = () => {
  const { containerId, urn } = useParams<{ containerId?: string; urn?: string }>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [selectedEntity, setSelectedEntity] = useState<LineageNode | null>(null);

  // New catalog-based lineage fetching
  const fetchLineageFromCatalog = async (entityUrn: string) => {
    try {
      setLoading(true);
      setError('');

      const response = await fetch(
        `http://localhost:3001/api/catalog/entities/${encodeURIComponent(entityUrn)}/lineage?direction=both&depth=3`
      );

      if (!response.ok) {
        throw new Error(`Catalog API error: ${response.status}`);
      }

      const data = await response.json();

      if (!data.nodes || data.nodes.length === 0) {
        setError('No lineage data found for this entity.');
        setLoading(false);
        return;
      }

      // Build ReactFlow graph
      const flowNodes: Node[] = data.nodes.map((node: any, idx: number) => ({
        id: node.urn,
        data: {
          label: node.name,
          type: node.entity_type,
          platform: node.platform
        },
        position: calculatePosition(idx, data.nodes.length),
        type: 'default',
        style: {
          background: getNodeColor(node.entity_type),
          border: '2px solid #333',
          padding: 10,
          borderRadius: 5,
          fontSize: 12
        }
      }));

      const flowEdges: Edge[] = data.edges.map((edge: any) => ({
        id: edge.edge_id,
        source: edge.source_urn,
        target: edge.target_urn,
        label: edge.edge_type,
        markerEnd: { type: MarkerType.ArrowClosed },
        style: { stroke: '#666', strokeWidth: 2 }
      }));

      setNodes(flowNodes);
      setEdges(flowEdges);
      setLoading(false);
    } catch (err: any) {
      console.error('Failed to fetch lineage from catalog:', err);
      setError(err.message || 'Failed to load lineage from catalog');
      setLoading(false);
    }
  };

  const getNodeColor = (entityType: string) => {
    switch (entityType) {
      case 'pipeline':
        return '#dbeafe';
      case 'job':
        return '#dcfce7';
      case 'dataset':
        return '#f3e8ff';
      case 'datasource':
        return '#fed7aa';
      default:
        return '#f3f4f6';
    }
  };

  const calculatePosition = (idx: number, total: number) => {
    const columns = Math.ceil(Math.sqrt(total));
    const row = Math.floor(idx / columns);
    const col = idx % columns;
    return {
      x: col * 200 + 50,
      y: row * 150 + 50
    };
  };

  const fetchLineageFromDataHub = async () => {
    try {
      setLoading(true);
      setError('');

      const DATAHUB_GRAPHQL = 'http://localhost:8080/api/graphql';

      // First, get all datasets for this container
      const searchQuery = `
        query {
          searchAcrossEntities(input: {types: [DATASET], query: "${containerId || '*'}", start: 0, count: 100}) {
            searchResults {
              entity {
                ... on Dataset {
                  urn
                  name
                  platform {
                    name
                  }
                  properties {
                    customProperties {
                      key
                      value
                    }
                  }
                }
              }
            }
          }
        }
      `;

      const response = await fetch(DATAHUB_GRAPHQL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ query: searchQuery })
      });

      if (!response.ok) {
        throw new Error(`DataHub API error: ${response.status}`);
      }

      const data = await response.json();
      
      if (data.errors) {
        throw new Error(data.errors[0]?.message || 'GraphQL query failed');
      }

      const results = data.data?.searchAcrossEntities?.searchResults || [];

      if (results.length === 0) {
        setError('No data found for this container. Try creating a NiFi flow first.');
        setLoading(false);
        return;
      }

      // Build lineage graph
      const flowNodes: Node[] = [];
      const flowEdges: Edge[] = [];
      const processedUrns = new Set<string>();

      // For each dataset, query its lineage
      for (let i = 0; i < results.length; i++) {
        const entity = results[i].entity;
        if (!entity || processedUrns.has(entity.urn)) continue;

        processedUrns.add(entity.urn);

        const customProps = entity.properties?.customProperties || [];
        const processorType = customProps.find((p: any) => p.key === 'processorType')?.value;

        // Add main node
        flowNodes.push({
          id: entity.urn,
          data: {
            label: (
              <div className="text-center">
                <div className="font-semibold">{entity.name}</div>
                {processorType && (
                  <div className="text-xs text-gray-500 mt-1">{processorType}</div>
                )}
              </div>
            )
          },
          position: { x: (i % 3) * 300 + 100, y: Math.floor(i / 3) * 200 + 100 },
          style: {
            background: 'white',
            border: '2px solid #0047AB',
            borderRadius: '8px',
            padding: '12px',
            fontSize: '14px',
            minWidth: '180px',
          },
        });

        // Query upstream lineage for this entity
        const lineageQuery = `
          query {
            searchAcrossLineage(input: {urn: "${entity.urn}", direction: UPSTREAM, start: 0, count: 10}) {
              searchResults {
                entity {
                  ... on Dataset {
                    urn
                    name
                  }
                }
              }
            }
          }
        `;

        const lineageResponse = await fetch(DATAHUB_GRAPHQL, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ query: lineageQuery })
        });

        if (lineageResponse.ok) {
          const lineageData = await lineageResponse.json();
          const upstreams = lineageData.data?.searchAcrossLineage?.searchResults || [];

          upstreams.forEach((upstream: any) => {
            if (upstream.entity && !processedUrns.has(upstream.entity.urn)) {
              processedUrns.add(upstream.entity.urn);
              
              flowNodes.push({
                id: upstream.entity.urn,
                data: {
                  label: (
                    <div className="text-center">
                      <div className="font-semibold">{upstream.entity.name}</div>
                    </div>
                  )
                },
                position: { x: 100, y: flowNodes.length * 150 },
                style: {
                  background: '#e6f2ff',
                  border: '2px solid #0047AB',
                  borderRadius: '8px',
                  padding: '12px',
                  fontSize: '13px',
                  minWidth: '160px',
                },
              });
            }

            if (upstream.entity) {
              flowEdges.push({
                id: `${upstream.entity.urn}-${entity.urn}`,
                source: upstream.entity.urn,
                target: entity.urn,
                animated: true,
                style: { stroke: '#0047AB', strokeWidth: 2 },
                markerEnd: {
                  type: MarkerType.ArrowClosed,
                  color: '#0047AB',
                },
                label: 'TRANSFORMED',
                labelStyle: { fill: '#0047AB', fontSize: 10 },
                labelBgStyle: { fill: 'white' },
              });
            }
          });
        }
      }

      setNodes(flowNodes);
      setEdges(flowEdges);
      setLoading(false);
    } catch (err: any) {
      console.error('Failed to fetch lineage:', err);
      setError(err.message || 'Failed to fetch lineage from DataHub. Make sure DataHub is running.');
      setLoading(false);
    }
  };

  useEffect(() => {
    // If URN is provided, use catalog API
    if (urn) {
      fetchLineageFromCatalog(urn);
    } else if (containerId) {
      // Otherwise fall back to DataHub (legacy)
      fetchLineageFromDataHub();
    }
  }, [containerId, urn]);

  const onNodeClick = (_event: any, node: Node) => {
    setSelectedEntity({
      urn: node.id,
      name: node.data.label as string,
      type: node.type || 'default',
      platform: 'nifi',
    });
  };

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <Loader2 className="w-12 h-12 text-cloudera-blue animate-spin mx-auto mb-4" />
          <p className="text-gray-600">Loading lineage from DataHub...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full flex items-center justify-center bg-gray-50">
        <div className="text-center max-w-md">
          <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Unable to Load Lineage</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={fetchLineageFromDataHub}
            className="px-4 py-2 bg-cloudera-blue text-white rounded hover:bg-blue-700 inline-flex items-center gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex">
      {/* Main Lineage View */}
      <div className="flex-1 relative">
        <div className="absolute top-4 left-4 z-10 bg-white rounded-lg shadow-lg p-4 border border-gray-200">
          <div className="flex items-center gap-2 mb-2">
            <GitGraph className="w-5 h-5 text-cloudera-blue" />
            <h3 className="font-semibold">Data Lineage</h3>
          </div>
          <p className="text-sm text-gray-600">
            Column-level lineage from NiFi flows
          </p>
        </div>

        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onNodeClick={onNodeClick}
          fitView
        >
          <Background />
          <Controls />
          <MiniMap />
        </ReactFlow>
      </div>

      {/* Sidebar - Entity Details */}
      {selectedEntity && (
        <div className="w-80 bg-white border-l border-gray-200 p-6 overflow-y-auto">
          <h3 className="text-lg font-semibold mb-4">Entity Details</h3>
          
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium text-gray-600">Name</label>
              <p className="text-gray-900">{selectedEntity.name}</p>
            </div>

            <div>
              <label className="text-sm font-medium text-gray-600">Type</label>
              <p className="text-gray-900">{selectedEntity.type}</p>
            </div>

            <div>
              <label className="text-sm font-medium text-gray-600">Platform</label>
              <p className="text-gray-900">{selectedEntity.platform}</p>
            </div>

            <div>
              <label className="text-sm font-medium text-gray-600">URN</label>
              <p className="text-xs text-gray-500 break-all font-mono">{selectedEntity.urn}</p>
            </div>

            <div className="pt-4 border-t border-gray-200">
              <h4 className="font-medium mb-2">Column Transformations</h4>
              <div className="space-y-2">
                <div className="bg-blue-50 p-3 rounded text-sm">
                  <p className="font-medium text-gray-700">Input Columns</p>
                  <p className="text-gray-600">• customer_id</p>
                  <p className="text-gray-600">• first_name</p>
                  <p className="text-gray-600">• last_name</p>
                </div>
                <div className="text-center text-gray-400">↓</div>
                <div className="bg-green-50 p-3 rounded text-sm">
                  <p className="font-medium text-gray-700">Output Columns</p>
                  <p className="text-gray-600">• cust_id (from customer_id)</p>
                  <p className="text-gray-600">• full_name (from first_name, last_name)</p>
                </div>
              </div>
            </div>

            <button
              onClick={() => window.open('http://localhost:9002', '_blank')}
              className="w-full px-4 py-2 bg-cloudera-blue text-white rounded hover:bg-blue-700"
            >
              View in DataHub
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default Lineage;
