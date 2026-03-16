import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Home from './pages/Home';
import Workspaces from './pages/Workspaces';
import WorkspaceCanvas from './pages/WorkspaceCanvas';
import Monitor from './pages/Monitor';
import RealTimeHub from './pages/RealTimeHub';
import DataCatalog from './pages/DataCatalog';
import CatalogSearch from './pages/CatalogSearch';
import EntityPage from './pages/EntityPage';
import UDFCatalogHome from './pages/UDFCatalogHome';
import UDFCatalogSearchV2 from './pages/UDFCatalogSearchV2';
import UDFEntityPage from './pages/UDFEntityPage';
import AtlasContainersView from './pages/AtlasContainersView';
import ToolEmbed from './pages/ToolEmbed';
import Lineage from './pages/Lineage';

function App() {
  return (
    <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/udf-catalog-search" element={<UDFCatalogSearchV2 />} />
          <Route path="/home" element={<Home />} />
          <Route path="/udf-catalog-home" element={<UDFCatalogHome />} />
          <Route path="/workspaces" element={<Workspaces />} />
          <Route path="/workspace/:id" element={<WorkspaceCanvas />} />
          <Route path="/workspace/:id/lineage" element={<Lineage />} />
          <Route path="/workspace/:id/atlas-containers" element={<AtlasContainersView />} />
          <Route path="/monitor" element={<Monitor />} />
          <Route path="/realtime" element={<RealTimeHub />} />
          <Route path="/catalog" element={<DataCatalog />} />
          <Route path="/catalog/search" element={<CatalogSearch />} />
          <Route path="/catalog/entity/:urn" element={<EntityPage />} />
          <Route path="/catalog/lineage/:urn" element={<Lineage />} />
          <Route path="/udf-catalog" element={<UDFCatalogHome />} />
          <Route path="/udf-catalog/search" element={<UDFCatalogSearchV2 />} />
          <Route path="/udf-catalog/entity/:urn" element={<UDFEntityPage />} />
          <Route path="/tool/:tool/:containerId" element={<ToolEmbed />} />
          <Route path="/lineage/:containerId" element={<Lineage />} />
        </Routes>
      </Layout>
    </Router>
  );
}

export default App;
