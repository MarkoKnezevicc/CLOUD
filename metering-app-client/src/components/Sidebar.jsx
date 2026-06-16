import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext'; // 1. Uvozimo naš novi useAuth hook

const Sidebar = () => {
  // 2. Izvlačimo user objekat i logout funkciju direktno iz globalnog stanja
  const { user, logout } = useAuth(); 

  return (
    <div className="sidebar" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <h3 style={{ marginTop: 0, color: '#ecf0f1' }}>Smart Grid</h3>
      
      {/* 3. Prikazujemo Ime i Prezime, a ako ih nema, email kao fallback */}
      <div style={{ marginBottom: '15px', fontSize: '14px', color: '#bdc3c7' }}>
        <p style={{ margin: '5px 0' }}>
          👤 Korisnik: <strong style={{ color: '#fff' }}>
            {user?.ime && user?.prezime ? `${user.ime} ${user.prezime}` : user?.email}
          </strong>
        </p>
      </div>
      <hr style={{ borderColor: '#34495e', width: '100%', marginBottom: '20px' }} />

      {/* 4. Dugme sada ispravno zove logout() iz AuthContext-a koji briše i token i ime/prezime */}
      <button 
        onClick={logout} 
        className="btn-logout"
        style={{
          marginTop: 'auto',
          backgroundColor: '#e74c3c',
          color: 'white',
          border: 'none',
          padding: '10px',
          borderRadius: '4px',
          cursor: 'pointer',
          fontWeight: 'bold'
        }}
      >
        Odjavi se
      </button>
    </div>
  );
};

export default Sidebar;