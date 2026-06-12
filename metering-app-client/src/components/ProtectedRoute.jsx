import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext'; // Koristimo naš novi AuthContext

const ProtectedRoute = ({ children, dozvoljeneUloge }) => {
  const { user, loading } = useAuth();

  
  if (loading) {
    return <div style={{ padding: '20px', textAlign: 'center' }}>Učitavanje...</div>;
  }

  
  if (!user) {
    return <Navigate to="/login" replace />;
  }

  
  if (dozvoljeneUloge && !dozvoljeneUloge.includes(user.uloga)) {
    return <Navigate to="/unauthorized" replace />;
  }

  
  return children;
};

export default ProtectedRoute;