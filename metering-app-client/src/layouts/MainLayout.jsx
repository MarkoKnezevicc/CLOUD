import React from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import GlobalNotificationCenter from '../components/GlobalNotificationCenter';
import { authService } from '../services/authService'; 

const MainLayout = () => {
  const navigate = useNavigate();

  const trenutniKorisnik = authService.getUser();
  const rola = trenutniKorisnik?.role || trenutniKorisnik?.uloga || 'Potrosac'; 
  const isSistemskiAdmin = rola === 'SistemskiAdmin';
  const emailKorisnika = trenutniKorisnik?.email || 'Korisnik';

  const handleLogout = () => {
    localStorage.clear(); 
    if (authService?.logout) authService.logout();
    navigate('/login');
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', fontFamily: 'Arial, sans-serif' }}>
      
      <div style={{ width: '260px', backgroundColor: '#2c3e50', color: 'white', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', padding: '20px 0' }}>
        <div>
          <h3 style={{ padding: '0 20px', margin: '0 0 20px 0', borderBottom: '1px solid #34495e', paddingBottom: '15px' }}>Smart Grid</h3>
          <div style={{ padding: '0 20px', fontSize: '14px', color: '#ecf0f1' }}>
            <span style={{ color: '#95a5a6', display: 'block', fontSize: '12px' }}>
              👤 {isSistemskiAdmin ? 'Sistemski Admin:' : 'Korisnik:'}
            </span>
            <strong>{emailKorisnika}</strong>
          </div>
        </div>

        <div style={{ padding: '0 20px' }}>
          <button 
            onClick={handleLogout}
            style={{ width: '100%', padding: '12px', backgroundColor: '#e74c3c', color: 'white', border: 'none', borderRadius: '4px', fontWeight: 'bold', cursor: 'pointer', transition: '0.2s' }}
            onMouseOver={(e) => e.target.style.backgroundColor = '#c0392b'}
            onMouseOut={(e) => e.target.style.backgroundColor = '#e74c3c'}
          >
            Odjavi se
          </button>
        </div>
      </div>

      <div style={{ flex: 1, backgroundColor: '#f8f9fa', display: 'flex', flexDirection: 'column' }}>
        {isSistemskiAdmin && <GlobalNotificationCenter />}

        <div style={{ padding: '30px', flex: 1 }}>
          <Outlet />
        </div>
      </div>

    </div>
  );
};

export default MainLayout;