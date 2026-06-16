import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';

const UserMeters = () => {
  const { userId } = useParams();
  const navigate = useNavigate();
  const [brojila, setBrojila] = useState([]);
  const [greska, setGreska] = useState('');
  const [loading, setLoading] = useState(true);

  const getHeaders = () => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authService.getToken()}`
  });

  useEffect(() => {
    const ucitajBrojila = async () => {
      try {
        const res = await fetch(`https://localhost:7078/api/admin/users/${userId}/meters`, { 
          headers: getHeaders() 
        });
        if (res.ok) {
          const data = await res.json();
          setBrojila(data);
        } else {
          setGreska('Neuspešno učitavanje brojila za izabranog korisnika.');
        }
      } catch (err) {
        setGreska('Greška pri povezivanju sa serverom.');
      } finally {
        setLoading(false);
      }
    };

    if (userId) ucitajBrojila();
  }, [userId]);

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <button 
          onClick={() => navigate(-1)} 
          style={{ padding: '8px 16px', cursor: 'pointer', background: '#6c757d', color: 'white', border: 'none', borderRadius: '4px', fontWeight: 'bold' }}
        >
          ⬅ Nazad na Korisnike
        </button>
        <h3 style={{ color: '#2c3e50', margin: 0 }}>Pregled Pametnih Brojila Korisnika</h3>
      </div>

      {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}

      {loading ? (
        <p>Učitavanje brojila...</p>
      ) : (
        <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #dee2e6', textAlign: 'left', backgroundColor: '#f8f9fa' }}>
                <th style={{ padding: '12px' }}>ID Brojila (GUID)</th>
                <th>Lokacija (Objekat)</th>
                <th>Adresa</th>
                <th>Tip Priključka</th>
                <th>Status</th>
                <th style={{ textAlign: 'center' }}>Akcija</th>
              </tr>
            </thead>
            <tbody>
              {brojila.map(b => (
                <tr key={b.id} style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '12px', fontFamily: 'monospace', fontSize: '13px' }}>{b.id}</td>
                  <td><strong>{b.nazivObjekta}</strong></td>
                  <td>{b.adresaObjekta}</td>
                  <td>
                    <span style={{ backgroundColor: '#e2e3e5', padding: '3px 8px', borderRadius: '3px', fontSize: '13px' }}>
                      {b.tip}
                    </span>
                  </td>
                  <td>
                    <span style={{ color: b.status === 'Uparen' ? '#28a745' : '#dc3545', fontWeight: 'bold' }}>
                      ● {b.status}
                    </span>
                  </td>
                  <td style={{ textAlign: 'center' }}>
                    <button
                      onClick={() => navigate(`/admin/telemetrija/${b.id}`)}
                      disabled={b.status !== 'Uparen'}
                      style={{
                        backgroundColor: b.status === 'Uparen' ? '#007bff' : '#6c757d',
                        color: 'white',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: b.status === 'Uparen' ? 'pointer' : 'not-allowed',
                        fontWeight: 'bold'
                      }}
                    >
                      Prati Uživo
                    </button>
                  </td>
                </tr>
              ))}

              {brojila.length === 0 && (
                <tr>
                  <td colSpan="6" style={{ textAlign: 'center', padding: '20px', color: '#6c757d' }}>
                    Ovaj korisnik nema dodeljenih pametnih brojila na svojim objektima.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default UserMeters;