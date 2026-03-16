import React, { useEffect, useRef, useState } from 'react';
import * as d3 from 'd3';
import { sankey, sankeyLinkHorizontal, SankeyNode, SankeyLink } from 'd3-sankey';

interface Column {
  name: string;
  type?: string;
}

interface Dataset {
  urn: string;
  name: string;
  platform: string;
  columns: Column[];
}

interface ColumnLineage {
  sourceColumn: string;
  targetColumn: string;
  transformationType?: string;
}

interface LineageData {
  source: Dataset;
  target: Dataset;
  columnLineages: ColumnLineage[];
}

interface SankeyLineageViewerProps {
  data: LineageData;
  width?: number;
  height?: number;
}

const SankeyLineageViewer: React.FC<SankeyLineageViewerProps> = ({ 
  data, 
  width = 1200, 
  height = 600 
}) => {
  const svgRef = useRef<SVGSVGElement>(null);
  const [hoveredLink, setHoveredLink] = useState<string | null>(null);

  useEffect(() => {
    if (!svgRef.current || !data) return;

    // Clear previous content
    d3.select(svgRef.current).selectAll('*').remove();

    const svg = d3.select(svgRef.current);
    const margin = { top: 20, right: 150, bottom: 20, left: 150 };
    const innerWidth = width - margin.left - margin.right;
    const innerHeight = height - margin.top - margin.bottom;

    const g = svg
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    // Prepare nodes and links for Sankey
    const nodes: any[] = [];
    const links: any[] = [];

    // Add source columns as nodes
    data.source.columns.forEach((col, idx) => {
      nodes.push({
        id: `source-${col.name}`,
        name: col.name,
        type: col.type,
        side: 'source',
        dataset: data.source.name,
        platform: data.source.platform
      });
    });

    // Add target columns as nodes
    data.target.columns.forEach((col, idx) => {
      nodes.push({
        id: `target-${col.name}`,
        name: col.name,
        type: col.type,
        side: 'target',
        dataset: data.target.name,
        platform: data.target.platform
      });
    });

    // Add links based on column lineages
    data.columnLineages.forEach((lineage) => {
      links.push({
        source: `source-${lineage.sourceColumn}`,
        target: `target-${lineage.targetColumn}`,
        value: 1,
        transformation: lineage.transformationType || 'IDENTITY'
      });
    });

    // Create Sankey layout
    const sankeyGenerator = sankey<any, any>()
      .nodeId((d: any) => d.id)
      .nodeWidth(200)
      .nodePadding(15)
      .extent([[0, 0], [innerWidth, innerHeight]]);

    const { nodes: sankeyNodes, links: sankeyLinks } = sankeyGenerator({
      nodes: nodes.map(d => ({ ...d })),
      links: links.map(d => ({ ...d }))
    });

    // Color scale for different transformation types
    const colorScale = d3.scaleOrdinal<string>()
      .domain(['IDENTITY', 'TRANSFORM', 'DERIVED'])
      .range(['#4F46E5', '#10B981', '#F59E0B']);

    // Draw links (the flowing arrows)
    const link = g.append('g')
      .attr('class', 'links')
      .selectAll('path')
      .data(sankeyLinks)
      .join('path')
      .attr('d', sankeyLinkHorizontal())
      .attr('stroke', (d: any) => colorScale(d.transformation))
      .attr('stroke-width', (d: any) => Math.max(1, d.width || 5))
      .attr('fill', 'none')
      .attr('opacity', (d: any) => {
        if (!hoveredLink) return 0.4;
        return hoveredLink === `${d.source.id}-${d.target.id}` ? 0.8 : 0.1;
      })
      .style('cursor', 'pointer')
      .on('mouseenter', function(event, d: any) {
        setHoveredLink(`${d.source.id}-${d.target.id}`);
        d3.select(this)
          .attr('opacity', 0.8)
          .attr('stroke-width', (d: any) => Math.max(3, (d.width || 5) * 1.5));
      })
      .on('mouseleave', function(event, d: any) {
        setHoveredLink(null);
        d3.select(this)
          .attr('opacity', 0.4)
          .attr('stroke-width', (d: any) => Math.max(1, d.width || 5));
      });

    // Add link labels (transformation type)
    g.append('g')
      .attr('class', 'link-labels')
      .selectAll('text')
      .data(sankeyLinks)
      .join('text')
      .attr('x', (d: any) => (d.source.x1 + d.target.x0) / 2)
      .attr('y', (d: any) => (d.y0 + d.y1) / 2)
      .attr('text-anchor', 'middle')
      .attr('font-size', '10px')
      .attr('fill', '#6B7280')
      .attr('opacity', (d: any) => hoveredLink === `${d.source.id}-${d.target.id}` ? 1 : 0)
      .text((d: any) => d.transformation);

    // Draw nodes (column boxes)
    const node = g.append('g')
      .attr('class', 'nodes')
      .selectAll('g')
      .data(sankeyNodes)
      .join('g')
      .attr('transform', (d: any) => `translate(${d.x0},${d.y0})`);

    // Node rectangles
    node.append('rect')
      .attr('width', (d: any) => d.x1 - d.x0)
      .attr('height', (d: any) => d.y1 - d.y0)
      .attr('fill', (d: any) => d.side === 'source' ? '#EEF2FF' : '#DBEAFE')
      .attr('stroke', (d: any) => d.side === 'source' ? '#4F46E5' : '#3B82F6')
      .attr('stroke-width', 2)
      .attr('rx', 6)
      .style('cursor', 'pointer')
      .on('mouseenter', function() {
        d3.select(this).attr('fill', (d: any) => d.side === 'source' ? '#E0E7FF' : '#BFDBFE');
      })
      .on('mouseleave', function() {
        d3.select(this).attr('fill', (d: any) => d.side === 'source' ? '#EEF2FF' : '#DBEAFE');
      });

    // Column names
    node.append('text')
      .attr('x', 10)
      .attr('y', (d: any) => (d.y1 - d.y0) / 2)
      .attr('dy', '0.35em')
      .attr('font-size', '13px')
      .attr('font-weight', '500')
      .attr('fill', '#1F2937')
      .text((d: any) => d.name);

    // Column types
    node.append('text')
      .attr('x', 10)
      .attr('y', (d: any) => (d.y1 - d.y0) / 2 + 14)
      .attr('font-size', '10px')
      .attr('fill', '#6B7280')
      .text((d: any) => d.type || '');

    // Dataset headers
    const sourceHeader = g.append('g')
      .attr('transform', `translate(0, -10)`);

    sourceHeader.append('text')
      .attr('x', 0)
      .attr('y', 0)
      .attr('font-size', '16px')
      .attr('font-weight', '600')
      .attr('fill', '#1F2937')
      .text(data.source.name);

    sourceHeader.append('text')
      .attr('x', 0)
      .attr('y', -18)
      .attr('font-size', '12px')
      .attr('fill', '#6B7280')
      .text(data.source.platform);

    const targetHeader = g.append('g')
      .attr('transform', `translate(${innerWidth - 200}, -10)`);

    targetHeader.append('text')
      .attr('x', 0)
      .attr('y', 0)
      .attr('font-size', '16px')
      .attr('font-weight', '600')
      .attr('fill', '#1F2937')
      .text(data.target.name);

    targetHeader.append('text')
      .attr('x', 0)
      .attr('y', -18)
      .attr('font-size', '12px')
      .attr('fill', '#6B7280')
      .text(data.target.platform);

    // Legend
    const legend = g.append('g')
      .attr('transform', `translate(${innerWidth / 2 - 150}, ${innerHeight + 30})`);

    const legendData = [
      { label: 'Direct Mapping', color: '#4F46E5' },
      { label: 'Transformation', color: '#10B981' },
      { label: 'Derived Column', color: '#F59E0B' }
    ];

    legendData.forEach((item, i) => {
      const legendItem = legend.append('g')
        .attr('transform', `translate(${i * 120}, 0)`);

      legendItem.append('line')
        .attr('x1', 0)
        .attr('y1', 0)
        .attr('x2', 30)
        .attr('y2', 0)
        .attr('stroke', item.color)
        .attr('stroke-width', 3);

      legendItem.append('text')
        .attr('x', 35)
        .attr('y', 0)
        .attr('dy', '0.35em')
        .attr('font-size', '12px')
        .attr('fill', '#6B7280')
        .text(item.label);
    });

  }, [data, width, height, hoveredLink]);

  return (
    <div className="sankey-lineage-viewer" style={{ 
      background: 'white', 
      borderRadius: '12px', 
      padding: '20px',
      boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
    }}>
      <svg 
        ref={svgRef} 
        width={width} 
        height={height}
        style={{ display: 'block' }}
      />
    </div>
  );
};

export default SankeyLineageViewer;
