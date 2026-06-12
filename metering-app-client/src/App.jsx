import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import MainLayout from './layouts/MainLayout';
import PotrosacDashboard from './pages/PotrosacDashboard';
import AdminDashboard from './pages/AdminDashboard';
import NaplataDashboard from './pages/NaplataDashboard';
import ProtectedRoute from './components/ProtectedRoute';
import Unauthorized from './pages/Unauthorized';

function App() {
  return (
    <Router>
      <Routes>
        
        <Route path="/login" element={<Login />} />
        <Route path="/unauthorized" element={<Unauthorized />} />

        
        <Route path="/" element={<MainLayout />}>
          
         
          <Route path="potrosac" element={
            <ProtectedRoute allowedRoles={['Potrosac']}>
              <PotrosacDashboard />
            </ProtectedRoute>
          } />

          
          <Route path="admin" element={
            <ProtectedRoute allowedRoles={['SistemskiAdmin']}>
              <AdminDashboard />
            </ProtectedRoute>
          } />

          
          <Route path="naplata" element={
            <ProtectedRoute allowedRoles={['AdministratorNaplate']}>
              <NaplataDashboard />
            </ProtectedRoute>
          } />

        </Route>

        
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Router>
  );
}

export default App;