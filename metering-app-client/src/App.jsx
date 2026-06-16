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

function App() {
  return (
    <Router>
      <Routes>
        
        {/* JAVNE RUTE */}
        <Route path="/login" element={<Login />} />
        <Route path="/unauthorized" element={<Unauthorized />} />

        {/* SVE RUTE UNUTAR MAINLAYOUT-A DELE ISTI MENI / NAVIGACIJU */}
        <Route path="/" element={<MainLayout />}>
          
          {/* RUTA ZA POTROŠAČA */}
          <Route path="potrosac" element={
            <ProtectedRoute allowedRoles={['Potrosac']}>
              <PotrosacDashboard />
            </ProtectedRoute>
          } />

          {/* RUTA ZA GLAVNI KONTROLNI PANEL ADMINA */}
          <Route path="admin" element={
            <ProtectedRoute allowedRoles={['SistemskiAdmin']}>
              <AdminDashboard />
            </ProtectedRoute>
          } />

          {/* ZAŠTIĆENA RUTA ZA PREGLED PAMETNIH BROJILA ODREĐENOG KORISNIKA */}
          <Route path="admin/users/:userId/meters" element={
            <ProtectedRoute allowedRoles={['SistemskiAdmin']}>
              <UserMeters />
            </ProtectedRoute>
          } />


          <Route path="admin/telemetrija/:brojiloId" element={
            <ProtectedRoute allowedRoles={['SistemskiAdmin']}>
              <LiveTelemetry />
            </ProtectedRoute>
          } />

          {/* RUTA ZA ADMINISTRATORA NAPLATE */}
          <Route path="naplata" element={
            <ProtectedRoute allowedRoles={['AdministratorNaplate']}>
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