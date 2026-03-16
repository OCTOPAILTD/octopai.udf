import config from '../config';

// Use relative URL so it works both in dev and production (via Nginx proxy)
const API_URL = window.location.port === '5173' 
  ? `${config.backendUrl}/api`  // Dev mode (Vite) - supports external access
  : '/api';  // Production mode (Nginx proxy)

export interface Container {
  id: string;
  name: string;
  image?: string;
  status: string;
  port?: string;
  url?: string;
  directUrl?: string;
  ports?: any;
  created?: string;
  workspace?: string;
  credentials?: {
    username: string;
    password: string;
  };
}

export const containerService = {
  // Create NiFi container
  async createNiFi(name: string, workspace: string): Promise<Container> {
    const response = await fetch(`${API_URL}/containers/nifi`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name, workspace }),
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to create NiFi container');
    }
    
    return data.container;
  },

  // Create Kafka container
  async createKafka(name: string, workspace: string): Promise<Container> {
    const response = await fetch(`${API_URL}/containers/kafka`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name, workspace }),
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to create Kafka container');
    }
    
    return data.container;
  },

  // Create Hive container with Hue editor
  async createHive(name: string, workspace: string): Promise<Container> {
    const response = await fetch(`${API_URL}/containers/hive`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name, workspace }),
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to create Hive container');
    }
    
    return data.container;
  },

  // Create Trino container
  async createTrino(name: string, workspace: string): Promise<Container> {
    // Create abort controller for timeout
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 120000); // 2 minute timeout
    
    try {
      const response = await fetch(`${API_URL}/containers/trino`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name, workspace }),
        signal: controller.signal,
      });
      
      clearTimeout(timeoutId);
      
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: `HTTP ${response.status}: ${response.statusText}` }));
        throw new Error(errorData.error || `Failed to create Trino container: ${response.statusText}`);
      }
      
      const data = await response.json();
      if (!data.success) {
        throw new Error(data.error || 'Failed to create Trino container');
      }
      
      return data.container;
    } catch (error: any) {
      clearTimeout(timeoutId);
      if (error.name === 'AbortError') {
        throw new Error('Request timeout: Container creation took too long. The container may still be creating in the background.');
      }
      throw error;
    }
  },

  // Create Impala container
  async createImpala(name: string, workspace: string): Promise<Container> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 120000);
    
    try {
      const response = await fetch(`${API_URL}/containers/impala`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name, workspace }),
        signal: controller.signal,
      });
      
      clearTimeout(timeoutId);
      
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: `HTTP ${response.status}: ${response.statusText}` }));
        throw new Error(errorData.error || `Failed to create Impala container: ${response.statusText}`);
      }
      
      const data = await response.json();
      if (!data.success) {
        throw new Error(data.error || 'Failed to create Impala container');
      }
      
      return data.container;
    } catch (error: any) {
      clearTimeout(timeoutId);
      if (error.name === 'AbortError') {
        throw new Error('Request timeout: Container creation took too long. The container may still be creating in the background.');
      }
      throw error;
    }
  },

  // Create HBase container
  async createHBase(name: string, workspace: string): Promise<Container> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 120000);
    
    try {
      const response = await fetch(`${API_URL}/containers/hbase`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name, workspace }),
        signal: controller.signal,
      });
      
      clearTimeout(timeoutId);
      
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: `HTTP ${response.status}: ${response.statusText}` }));
        throw new Error(errorData.error || `Failed to create HBase container: ${response.statusText}`);
      }
      
      const data = await response.json();
      if (!data.success) {
        throw new Error(data.error || 'Failed to create HBase container');
      }
      
      return data.container;
    } catch (error: any) {
      clearTimeout(timeoutId);
      if (error.name === 'AbortError') {
        throw new Error('Request timeout: Container creation took too long. The container may still be creating in the background.');
      }
      throw error;
    }
  },

  // List all containers
  async listContainers(): Promise<Container[]> {
    const response = await fetch(`${API_URL}/containers`);
    if (!response.ok) {
      console.error(`[containerService] Failed to fetch containers: ${response.status} ${response.statusText}`);
      return [];
    }
    const data = await response.json();
    console.log(`[containerService] Received ${data.containers?.length || 0} containers from API`);
    if (data.containers && data.containers.length > 0) {
      console.log(`[containerService] Container workspaces:`, data.containers.map((c: Container) => ({ name: c.name, workspace: c.workspace })));
    }
    return data.containers || [];
  },

  // Start container
  async startContainer(id: string): Promise<void> {
    const response = await fetch(`${API_URL}/containers/${id}/start`, {
      method: 'POST',
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to start container');
    }
  },

  // Stop container
  async stopContainer(id: string): Promise<void> {
    const response = await fetch(`${API_URL}/containers/${id}/stop`, {
      method: 'POST',
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to stop container');
    }
  },

  // Remove container
  async removeContainer(id: string): Promise<void> {
    const response = await fetch(`${API_URL}/containers/${id}`, {
      method: 'DELETE',
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to remove container');
    }
  },

  // Create DataHub stack
  async createDataHub(name: string, workspace: string): Promise<Container> {
    const response = await fetch(`${API_URL}/containers/datahub`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name, workspace }),
    });
    
    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to create DataHub stack');
    }
    
    return data.container;
  },

  // Get container details
  async getContainer(id: string): Promise<Container | null> {
    try {
      const containers = await this.listContainers();
      const container = containers.find(c => c.id === id);
      
      if (!container) {
        return null;
      }
      
      // Build URLs based on container image type
      let url = '';
      let directUrl = '';
      
      if (container.image && container.image.includes('nifi')) {
        const port = container.ports?.find((p: any) => p.PrivatePort === 8080)?.PublicPort;
        if (port) {
          url = `http://localhost:3001/api/proxy/${id}/nifi/`;
          directUrl = `http://localhost:${port}/nifi/`;
        }
      } else if (container.image && container.image.includes('hue')) {
        // Hue SQL Editor port is 8888
        const port = container.ports?.find((p: any) => p.PrivatePort === 8888)?.PublicPort;
        if (port) {
          url = `http://localhost:3001/api/proxy/${id}/`;
          directUrl = `http://localhost:${port}`;
        }
      } else if (container.image && container.image.includes('datahub')) {
        // DataHub frontend port is 9002
        const port = container.ports?.find((p: any) => p.PrivatePort === 9002)?.PublicPort;
        if (port) {
          url = `http://localhost:3001/api/proxy/${id}/`;
          directUrl = `http://localhost:${port}`;
        }
      }
      
      return {
        ...container,
        url,
        directUrl
      };
    } catch (error) {
      console.error('Failed to get container details:', error);
      return null;
    }
  },

  // Get NiFi changes for DataHub ingestion
  async getNiFiChanges(id: string): Promise<any[]> {
    try {
      const response = await fetch(`${API_URL}/containers/${id}/nifi-changes`);
      const data = await response.json();
      return data.changes || [];
    } catch (error) {
      console.error('Failed to get NiFi changes:', error);
      return [];
    }
  },

  // Check container health
  async checkHealth(id: string): Promise<{ status: string; ready: boolean; nifi_ready?: boolean }> {
    try {
      const response = await fetch(`${API_URL}/containers/${id}/health`);
      const data = await response.json();
      return data;
    } catch (error) {
      console.error('Failed to check container health:', error);
      return { status: 'unknown', ready: false };
    }
  },
};
