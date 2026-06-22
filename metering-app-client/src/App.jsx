import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import MainLayout from './layouts/MainLayout';
import PotrosacDashboard from './pages/PotrosacDashboard';
import AdminDashboard from './pages/AdminDashboard';
import NaplataDashboard from './pages/NaplataDashboard';
import ProtectedRoute from './components/ProtectedRoute';
import Unauthorized from './pages/Unauthorized';
import LiveTelemetry from './pages/LiveTelemetry';
import UserMeters from './pages/UserMeters';
import PlacanjeUspesno from './pages/PlacanjeUspesno';
import PlacanjeOtkazano from './pages/PlacanjeOtkazano';
import { useAuth } from './context/AuthContext';

const PocetnaRuta = () => {
  const { user } = useAuth();

  if (!user) return <Navigate to="/login" replace />;
  if (user.uloga === 'SistemskiAdmin') return <Navigate to="/admin" replace />;
  if (user.uloga === 'AdministratorNaplate') return <Navigate to="/naplata" replace />;
  return <Navigate to="/potrosac" replace />;
};

function App() {
  return (
    <Router>
      <Routes>

        {/* JAVNE RUTE */}
        <Route path="/login" element={<Login />} />
        <Route path="/unauthorized" element={<Unauthorized />} />
        <Route path="/placanje-uspesno" element={<PlacanjeUspesno />} />
        <Route path="/placanje-otkazano" element={<PlacanjeOtkazano />} />

        {/* SVE RUTE UNUTAR MAINLAYOUT-A DELE ISTI MENI / NAVIGACIJU */}
        <Route path="/" element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>

          <Route index element={<PocetnaRuta />} />

          {/* RUTA ZA POTROŠAČA */}
          <Route path="potrosac" element={
            <ProtectedRoute dozvoljeneUloge={['Potrosac']}>
              <PotrosacDashboard />
            </ProtectedRoute>
          } />

          {/* RUTA ZA GLAVNI KONTROLNI PANEL ADMINA */}
          <Route path="admin" element={
            <ProtectedRoute dozvoljeneUloge={['SistemskiAdmin']}>
              <AdminDashboard />
            </ProtectedRoute>
          } />

          {/* ZAŠTIĆENA RUTA ZA PREGLED PAMETNIH BROJILA ODREĐENOG KORISNIKA */}
          <Route path="admin/users/:userId/meters" element={
            <ProtectedRoute dozvoljeneUloge={['SistemskiAdmin']}>
              <UserMeters />
            </ProtectedRoute>
          } />


          <Route path="admin/telemetrija/:brojiloId" element={
            <ProtectedRoute dozvoljeneUloge={['SistemskiAdmin']}>
              <LiveTelemetry />
            </ProtectedRoute>
          } />

          {/* RUTA ZA ADMINISTRATORA NAPLATE */}
          <Route path="naplata" element={
            <ProtectedRoute dozvoljeneUloge={['AdministratorNaplate']}>
              <NaplataDashboard />
            </ProtectedRoute>
          } />

        </Route>

        {/* PREUSMERAVANJE UKOLIKO RUTA NE POSTOJI */}
        <Route path="*" element={<Navigate to="/login" replace />} />
        
      </Routes>
    </Router>
  );
}

export default App;
