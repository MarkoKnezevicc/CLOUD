import React, { createContext, useState, useEffect, useContext } from 'react';
import { authService } from '../services/authService';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);


  useEffect(() => {
    const osveziKorisnika = () => {
      const ulogovanKorisnik = authService.getUser();
      setUser(ulogovanKorisnik);
      setLoading(false);
    };

    osveziKorisnika();
    
   
    window.addEventListener('storage', osveziKorisnika);
    return () => window.removeEventListener('storage', osveziKorisnika);
  }, []);


  const login = (token) => {
    authService.saveToken(token);
    const ulogovanKorisnik = authService.getUser();
    setUser(ulogovanKorisnik);
    return ulogovanKorisnik;
  };

  const logout = () => {
    localStorage.removeItem("token");
    setUser(null);
    window.location.href = '/login';
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, loading }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};


export const useAuth = () => {
  return useContext(AuthContext);
};