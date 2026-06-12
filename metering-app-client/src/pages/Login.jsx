import React, { useState } from 'react';
import { authService } from '../services/authService';

const Login = () => {
  const [email, setEmail] = useState('');
  const [lozinka, setLozinka] = useState('');
  const [greska, setGreska] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setGreska('');

    try {
      // PROVERI PORT: Promeni 7025 u tačan port tvog C# API-ja!
      const response = await fetch('https://localhost:7078/api/autentifikacija/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, lozinka }),
      });

      if (!response.ok) {
        const errData = await response.json();
        throw new Error(errData.poruka || 'Pogrešan email ili lozinka!');
      }

      const data = await response.json(); // Očekujemo { token: "ey..." }
      
      authService.saveToken(data.token);

      // Pročitamo ulogu iz tek sačuvanog tokena i preusmerimo korisnika
      const user = authService.getUser();
      if (user.uloga === 'Potrosac') window.location.href = '/potrosac';
      else if (user.uloga === 'SistemskiAdmin') window.location.href = '/admin';
      else if (user.uloga === 'AdministratorNaplate') window.location.href = '/naplata';
      else window.location.href = '/unauthorized';

    } catch (err) {
      setGreska(err.message);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', backgroundColor: '#e9ecef' }}>
      <form onSubmit={handleSubmit} style={{ backgroundColor: 'white', padding: '40px', borderRadius: '8px', boxShadow: '0 4px 6px rgba(0,0,0,0.1)', width: '100%', maxWidth: '400px' }}>
        <h3 style={{ textAlign: 'center', marginBottom: '20px' }}>Prijava na sistem</h3>
        
        {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px', textAlign: 'center' }}>{greska}</div>}
        
        <div style={{ marginBottom: '15px' }}>
          <label style={{ display: 'block', marginBottom: '5px' }}>Email adresa:</label>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required style={{ width: '94%', padding: '10px', borderRadius: '4px', border: '1px solid #ced4da' }} />
        </div>

        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px' }}>Lozinka:</label>
          <input type="password" value={lozinka} onChange={(e) => setLozinka(e.target.value)} required style={{ width: '94%', padding: '10px', borderRadius: '4px', border: '1px solid #ced4da' }} />
        </div>

        <button type="submit" style={{ width: '100%', padding: '12px', backgroundColor: '#007bff', color: 'white', border: 'none', borderRadius: '4px', fontSize: '16px', cursor: 'pointer' }}>
          Prijavi se
        </button>
      </form>
    </div>
  );
};

export default Login;