import React, { useState, useEffect } from 'react';
import { authService } from '../services/authService';
import { useAuth } from '../context/AuthContext'; 
import { useNavigate } from 'react-router-dom';

const AdminDashboard = () => {
  const [users, setUsers] = useState([]);
  const [greska, setGreska] = useState('');
  

  const { user } = useAuth();
  const navigate = useNavigate();
  

  const [noviUser, setNoviUser] = useState({ ime: '', prezime: '', email: '', telefon: '', lozinka: '', uloga: 1 });

  const getHeaders = () => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authService.getToken()}`
  });

  const ucitajUsere = async () => {
    try {
      const res = await fetch('https://localhost:7078/api/admin/users', { headers: getHeaders() });
      if (res.ok) {
        const data = await res.json();
        setUsers(data);
      } else {
        setGreska('Nemate ovlašćenje za pregled korisnika.');
      }
    } catch (err) {
      setGreska('Greška pri povezivanju sa serverom.');
    }
  };

  useEffect(() => { ucitajUsere(); }, []);


  const handleDodajUsera = async (e) => {
    e.preventDefault();
    setGreska('');
    try {
      const res = await fetch('https://localhost:7078/api/admin/users', {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({
          ime: noviUser.ime,
          prezime: noviUser.prezime,
          email: noviUser.email,
          telefon: noviUser.telefon,
          lozinka: noviUser.lozinka,
          uloga: parseInt(noviUser.uloga)
        })
      });

      if (res.ok) {
        setNoviUser({ ime: '', prezime: '', email: '', telefon: '', lozinka: '', uloga: 1 });
        ucitajUsere();
        alert('Korisnik uspešno kreiran!');
      } else {
        const errData = await res.json();
        setGreska(errData.poruka || 'Neuspešno dodavanje korisnika.');
      }
    } catch (err) {
      setGreska('Sistemska greška pri slanju podataka.');
    }
  };


  const handleToggleStatus = async (id, trenutniStatus) => {
    try {
      const res = await fetch(`https://localhost:7078/api/admin/users/${id}/status`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify({ isActive: !trenutniStatus }) 
      });
      if (res.ok) ucitajUsere();
    } catch (err) {
      console.error(err);
    }
  };


  const handleObrisi = async (id) => {
    if (window.confirm("Da li ste sigurni da želite da obrišete ovog korisnika iz sistema?")) {
      try {
        const res = await fetch(`https://localhost:7078/api/admin/users/${id}`, {
          method: 'DELETE',
          headers: getHeaders()
        });
        if (res.ok) ucitajUsere();
      } catch (err) {
        console.error(err);
      }
    }
  };

  const prikazaniKorisnici = users.filter(
    u => u.email.toLowerCase() !== user?.email?.toLowerCase()
  );

  return (
    <div style={{ fontFamily: 'Arial, sans-serif' }}>
      <h3 style={{ color: '#2c3e50', borderBottom: '2px solid #34495e', paddingBottom: '10px' }}>⚙️ Kontrolni Panel - Upravljanje Korisnicima</h3>
      
      {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}

      {/* FORMA ZA DODAVANJE KORISNIKA */}
      <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', marginBottom: '30px' }}>
        <h4>Novi Korisnički Nalog</h4>
        <form onSubmit={handleDodajUsera} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
          <input type="text" placeholder="Ime" value={noviUser.ime} onChange={e => setNoviUser({...noviUser, ime: e.target.value})} required style={{ padding: '8px' }} />
          <input type="text" placeholder="Prezime" value={noviUser.prezime} onChange={e => setNoviUser({...noviUser, prezime: e.target.value})} required style={{ padding: '8px' }} />
          <input type="email" placeholder="Email adresa" value={noviUser.email} onChange={e => setNoviUser({...noviUser, email: e.target.value})} required style={{ padding: '8px' }} />
          <input type="text" placeholder="Telefon" value={noviUser.telefon} onChange={e => setNoviUser({...noviUser, telefon: e.target.value})} style={{ padding: '8px' }} />
          <input type="password" placeholder="Lozinka" value={noviUser.lozinka} onChange={e => setNoviUser({...noviUser, lozinka: e.target.value})} required style={{ padding: '8px' }} />
          
          <select value={noviUser.uloga} onChange={e => setNoviUser({...noviUser, uloga: e.target.value})} style={{ padding: '8px' }}>
            <option value={1}>Potrošač</option>
            <option value={2}>Administrator Naplate</option>
          </select>

          <button type="submit" style={{ gridColumn: 'span 2', backgroundColor: '#28a745', color: 'white', border: 'none', padding: '10px', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>
            Registruj Korisnika
          </button>
        </form>
      </div>

      {/* TABELA KORISNIKA */}
      <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
        <h4>Registrovani Korisnici u Sistemu</h4>
        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '15px' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #dee2e6', textAlign: 'left', backgroundColor: '#f8f9fa' }}>
              <th style={{ padding: '12px' }}>Ime i Prezime</th>
              <th>Email</th>
              <th>Uloga</th>
              <th>Status</th>
              <th style={{ textAlign: 'center' }}>Akcije</th>
            </tr>
          </thead>
          <tbody>
            {/* PROMENJENO: Mapiramo kroz prikazaniKorisnici umesto kroz users */}
            {prikazaniKorisnici.map(u => (
              <tr key={u.id} style={{ borderBottom: '1px solid #dee2e6' }}>
                <td style={{ padding: '12px' }}>
                  <span 
                    onClick={() => navigate(`/admin/users/${u.id}/meters`)}
                    style={{ 
                      color: '#007bff', 
                      textDecoration: 'underline', 
                      cursor: 'pointer', 
                      fontWeight: '500' 
                    }}
                  >
                    {u.ime} {u.prezime}
                  </span>
                </td>
                <td>{u.email}</td>
                <td><span style={{ backgroundColor: '#e2e3e5', padding: '3px 8px', borderRadius: '3px', fontSize: '14px' }}>{u.uloga}</span></td>
                <td>
                  <span style={{ color: u.isActive ? '#28a745' : '#dc3545', fontWeight: 'bold' }}>
                    {u.isActive ? '● Aktivan' : '○ Suspendovan'}
                  </span>
                </td>
                <td style={{ textAlign: 'center' }}>
                  <button 
                    onClick={() => handleToggleStatus(u.id, u.isActive)}
                    style={{ backgroundColor: u.isActive ? '#ffc107' : '#17a2b8', color: 'black', border: 'none', padding: '6px 12px', borderRadius: '4px', cursor: 'pointer', marginRight: '8px' }}
                  >
                    {u.isActive ? 'Suspenduj' : 'Aktiviraj'}
                  </button>
                  <button 
                    onClick={() => handleObrisi(u.id)}
                    style={{ backgroundColor: '#dc3545', color: 'white', border: 'none', padding: '6px 12px', borderRadius: '4px', cursor: 'pointer' }}
                  >
                    Obriši
                  </button>
                </td>
              </tr>
            ))}
            
            {/* Poruka ukoliko nema drugih korisnika osim tebe */}
            {prikazaniKorisnici.length === 0 && (
              <tr>
                <td colSpan="5" style={{ textAlign: 'center', padding: '20px', color: '#6c757d' }}>
                  Nema drugih korisnika u bazi.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminDashboard;