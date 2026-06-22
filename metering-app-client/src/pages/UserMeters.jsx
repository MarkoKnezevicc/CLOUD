import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import * as signalR from '@microsoft/signalr'; 

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
          
          const inicijalizovano = data.map(b => ({
            ...b,
            mrezniStatus: 'offline',
            poslednjeVidjenje: null
          }));
          
          setBrojila(inicijalizovano);
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

  useEffect(() => {
  
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:7071/api") 
    .withAutomaticReconnect()
    .build();

  connection.start()
    .then(() => {
      console.log("Tabela brojila se uspešno pretplatila na SveMrezneAktivnosti kanal!");
      
      
      fetch('http://localhost:7071/api/joinGroup', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          connectionId: connection.connectionId, 
          groupName: "SveMrezneAktivnosti" 
        })
      }).catch(err => console.error("Greška pri joinGroup:", err));
    })
    .catch(err => console.error("SignalR greska na tabeli:", err));

 
  connection.on("MrezniHeartbeatStigao", (data) => {
    setBrojila(prethodnaBrojila => 
      prethodnaBrojila.map(b => {
        if (b.id === data.brojiloId) {
          return {
            ...b,
            mrezniStatus: 'online',
            poslednjeVidjenje: new Date()
          };
        }
        return b;
      })
    );
  });

  return () => {
    connection.stop();
  };
}, []);

  useEffect(() => {
    const interval = setInterval(() => {
      const sada = new Date();
      
      setBrojila(prethodnaBrojila => 
        prethodnaBrojila.map(b => {
          
          if (!b.poslednjeVidjenje) return b;

          
          if (sada - b.poslednjeVidjenje > 15000 && b.mrezniStatus === 'online') {
  
  
            const kvarEvent = new CustomEvent('lokalniKvarUpozorenje', {
              detail: {
                brojiloId: b.id,
                adresa: b.adresaObjekta || 'Nepoznata adresa'
              }
            });
            window.dispatchEvent(kvarEvent);

            return {
              ...b,
              mrezniStatus: 'offline' 
            };
          }
          return b;
        })
      );
    }, 500);

    return () => clearInterval(interval);
  }, []);

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
                <th>Status uparivanja</th>
                <th>Mrežni Status uživo</th>
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
                  
                  
                  <td>
                    <span style={{ 
                      color: b.mrezniStatus === 'online' ? '#28a745' : '#dc3545', 
                      fontWeight: 'bold',
                      backgroundColor: b.mrezniStatus === 'online' ? '#e8f5e9' : '#ffebee',
                      padding: '4px 10px',
                      borderRadius: '12px',
                      fontSize: '12px'
                    }}>
                      ● {b.mrezniStatus.toUpperCase()}
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
                  <td colSpan="7" style={{ textAlign: 'center', padding: '20px', color: '#6c757d' }}>
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