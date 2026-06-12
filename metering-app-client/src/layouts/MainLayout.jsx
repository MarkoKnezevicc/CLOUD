import React from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import { useAuth } from '../context/AuthContext';

const MainLayout = () => {
  const { user } = useAuth(); // Povlačenje iz globalnog stanja

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div style={{ display: 'flex', minHeight: '100vh', fontFamily: 'Arial, sans-serif' }}>
      {/* Levi deo: Meni */}
      <div style={{ width: '250px', backgroundColor: '#2c3e50', color: 'white', padding: '20px' }}>
        <Sidebar />
      </div>

      {/* Desni deo: Glavni sadržaj stranice */}
      <div style={{ flex: 1, padding: '30px', backgroundColor: '#f8f9fa' }}>
        <header style={{ borderBottom: '1px solid #dee2e6', paddingBottom: '15px', marginBottom: '20px' }}>
          <h2>Smart Grid Dashboard</h2>
          <span style={{ color: '#6c757d' }}>Ulogovani ste kao: <strong>{user.uloga}</strong></span>
        </header>
        
        <main>
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default MainLayout;