// API Configuration
// Automatically uses the current host for external access support

const getBackendUrl = (): string => {
  // If VITE_BACKEND_URL is set, use it
  const envBackendUrl = import.meta.env.VITE_BACKEND_URL;
  if (envBackendUrl) {
    return envBackendUrl;
  }
  
  // Otherwise, use the current hostname with backend port
  // This allows external access from other machines
  const hostname = window.location.hostname;
  return `http://${hostname}:5000`;
};

export const config = {
  backendUrl: getBackendUrl(),
};

export default config;
