import React, { useState, useEffect } from 'react';
import { authService } from '../services/authService';

const PotrosacDashboard = () => {
  const [objekti, setObjekti] = useState([]);
  const [greska, setGreska] = useState('');
  const [poruka, setPoruka] = useState('');
  // Dodato
  const [aktivnoBrojiloZaRacune, setAktivnoBrojiloZaRacune] = useState(null);
  const [racuni, setRacuni] = useState([]);
  const [ucitavamRacune, setUcitavamRacune] = useState(false);

  // Limit potrosnje
  const [aktivnoBrojiloZaLimit, setAktivnoBrojiloZaLimit] = useState(null);
  const [limitVrednost, setLimitVrednost] = useState('');
  const [limitJedinica, setLimitJedinica] = useState('KWh');

  // Stanje za formu novog objekta
  const [noviObjekat, setNoviObjekat] = useState({ naziv: '', grad: '', adresa: '', opis: '' });

  // Pratimo samo koji objekat ima otvorenu formu za brojilo
  const [aktivniObjekatId, setAktivniObjekatId] = useState(null);

  const getHeaders = () => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authService.getToken()}`
  });

  // 1. Učitavanje svih objekata i brojila
  const ucitajObjekte = async () => {
    try {
      const res = await fetch('https://localhost:7078/api/potrosac/objekti', { headers: getHeaders() });
      if (res.ok) {
        const data = await res.json();
        setObjekti(data);
      } else {
        setGreska('Neuspešno učitavanje objekata.');
      }
    } catch (err) {
      setGreska('Greška pri komunikaciji sa serverom.');
    }
  };

  // Ucitavanje racuna 
  const ucitajRacune = async (brojiloId) => {
    if (aktivnoBrojiloZaRacune === brojiloId) {
      setAktivnoBrojiloZaRacune(null);
      setRacuni([]);
      return;
    }
    setUcitavamRacune(true);
    setAktivnoBrojiloZaRacune(brojiloId);
    try {
      const res = await fetch(`https://localhost:7078/api/obracun/racuni/${brojiloId}`, { headers: getHeaders() });
      if (res.ok) setRacuni(await res.json());
      else setGreska('Greska pri ucitavanju racuna.');
    } catch {
      setGreska('Sistemska greska.');
    } finally {
      setUcitavamRacune(false);
    }
  };

  const otvoriFormuLimita = (brojilo) => {
  if (aktivnoBrojiloZaLimit === brojilo.id) {
    setAktivnoBrojiloZaLimit(null);
    return;
  }
  setAktivnoBrojiloZaLimit(brojilo.id);
  setLimitVrednost(brojilo.limitVrednost ?? '');
  setLimitJedinica(brojilo.limitJedinica ?? 'KWh');
};

const sacuvajLimit = async (brojiloId) => {
  try {
    const res = await fetch(`https://localhost:7078/api/potrosac/brojila/${brojiloId}/limit`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({
        LimitVrednost: limitVrednost === '' ? null : parseFloat(limitVrednost),
        LimitJedinica: limitJedinica
      })
    });
    if (res.ok) {
      setPoruka('Limit potrošnje sačuvan!');
      setAktivnoBrojiloZaLimit(null);
      ucitajObjekte();
    } else {
      const err = await res.json();
      setGreska(err.poruka || 'Greška pri čuvanju limita.');
    }
  } catch {
    setGreska('Sistemska greška.');
  }
};

// Limit potrosnje
const ukloniLimit = async (brojiloId) => {
  try {
    const res = await fetch(`https://localhost:7078/api/potrosac/brojila/${brojiloId}/limit`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ LimitVrednost: null, LimitJedinica: null })
    });
    if (res.ok) {
      setPoruka('Limit uklonjen!');
      setAktivnoBrojiloZaLimit(null);
      ucitajObjekte();
    }
  } catch {
    setGreska('Sistemska greška.');
  }
};

  // Pokretanje Stripe placanja
  const platiRacun = async (racunId) => {
    try {
      const res = await fetch(`https://localhost:7078/api/placanje/kreiraj-sesiju/${racunId}`, {
        method: 'POST',
        headers: getHeaders()
      });
      if (res.ok) {
        const data = await res.json();
        window.location.href = data.url;
      } else {
        setGreska('Greska pri pokretanju placanja.');
      }
    } catch {
      setGreska('Sistemska greska.');
    }
  };

  useEffect(() => { ucitajObjekte(); }, []);

  // 2. Dodavanje novog objekta
  const handleDodajObjekat = async (e) => {
    e.preventDefault();
    setGreska(''); setPoruka('');
    try {
      const res = await fetch('https://localhost:7078/api/potrosac/objekti', {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({
          Naziv: noviObjekat.naziv,
          Grad: noviObjekat.grad,
          Adresa: noviObjekat.adresa,
          Opis: noviObjekat.opis
        })
      });

      if (res.ok) {
        setPoruka('Objekat uspešno kreiran!');
        setNoviObjekat({ naziv: '', grad: '', adresa: '', opis: '' });
        ucitajObjekte();
      } else {
        const errData = await res.json();
        setGreska(errData.poruka || 'Greška pri dodavanju objekta.');
      }
    } catch (err) {
      setGreska('Sistemska greška.');
    }
  };

  // 3. Registracija pametnog brojila (Samo Serijski Broj, Tip i Napomena)
  const handleRegistrujBrojilo = async (e, objekatId) => {
    e.preventDefault();
    setGreska(''); 
    setPoruka('');

    const formData = new FormData(e.target);

    // OPTIMIZOVAN PAYLOAD: Bez Oznake brojila
    const payload = {
      SerijskiBroj: formData.get("serijskiBroj"),
      Tip: formData.get("tip"),
      Napomena: formData.get("napomena") || ""
    };

    console.log("--- DEBUG REAL PAYLOAD ---");
    console.log("Šaljem na backend za objekat ID " + objekatId + ":", payload);

    try {
      const res = await fetch(`https://localhost:7078/api/potrosac/objekti/${objekatId}/brojila`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(payload) 
      });

      if (res.ok) {
        setPoruka('Brojilo uspešno registrovano! Status: Neuparen.');
        setAktivniObjekatId(null); 
        ucitajObjekte(); 
      } else {
        const errData = await res.json();
        if (errData.errors) {
            setGreska(Object.values(errData.errors).flat().join(" "));
        } else {
            setGreska(errData.poruka || 'Greška pri registraciji brojila.');
        }
      }
    } catch (err) {
      setGreska('Sistemska greška.');
    }
  };

  return (
    <div style={{ fontFamily: 'Arial, sans-serif', padding: '20px' }}>
      <h3 style={{ color: '#2c3e50', borderBottom: '2px solid #34495e', paddingBottom: '10px' }}>
        🏠 Moji Objekti i Pametna Brojila
      </h3>

      {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}
      {poruka && <div style={{ color: 'green', backgroundColor: '#d4edda', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{poruka}</div>}

      {/* FORMA ZA DODAVANJE OBJEKTA */}
      <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', marginBottom: '30px' }}>
        <h4>Dodaj novi objekat (Stan, Kuća, Vikendica...)</h4>
        <form onSubmit={handleDodajObjekat} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
          <input type="text" placeholder="Naziv (npr. Stan u centru)" value={noviObjekat.naziv} onChange={e => setNoviObjekat({...noviObjekat, naziv: e.target.value})} required style={{ padding: '8px' }} />
          <input type="text" placeholder="Grad" value={noviObjekat.grad} onChange={e => setNoviObjekat({...noviObjekat, grad: e.target.value})} required style={{ padding: '8px' }} />
          <input type="text" placeholder="Adresa" value={noviObjekat.adresa} onChange={e => setNoviObjekat({...noviObjekat, adresa: e.target.value})} required style={{ padding: '8px' }} />
          <input type="text" placeholder="Dodatni opis (opciono)" value={noviObjekat.opis} onChange={e => setNoviObjekat({...noviObjekat, opis: e.target.value})} style={{ padding: '8px' }} />
          
          <button type="submit" style={{ gridColumn: 'span 2', backgroundColor: '#28a745', color: 'white', border: 'none', padding: '10px', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>
            Sačuvaj Objekat
          </button>
        </form>
      </div>

      {/* PRIKAZ OBJEKATA I NJIHOVIH BROJILA */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {objekti.map(obj => (
          <div key={obj.id} style={{ backgroundColor: 'white', border: '1px solid #dee2e6', borderRadius: '8px', padding: '20px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
            <h4 style={{ marginTop: 0, color: '#007bff' }}>{obj.naziv} <span style={{ color: '#6c757d', fontSize: '14px' }}>- {obj.adresa}, {obj.grad}</span></h4>
            
            {/* Tabela sa brojilima za ovaj objekat */}
            {obj.brojila && obj.brojila.length > 0 ? (
              <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '15px', marginBottom: '15px' }}>
                <thead>
                  <tr style={{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6', textAlign: 'left' }}>
                    <th style={{ padding: '10px' }}>Serijski Broj</th>
                    <th>Tip Priključka</th>
                    <th>Max Snaga (kW)</th>
                    <th>Status</th>
                    <th>Limit potrošnje</th>
                  </tr>
                </thead>
                <tbody>
                  {obj.brojila.map(b => (
                    <React.Fragment key = {b.id}>
                      <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                      <td style={{ padding: '10px', fontFamily: 'monospace', fontWeight: 'bold' }}>{b.serijskiBroj}</td>
                      <td>{b.tip}</td>
                      <td>{b.maksimalnaOdobrenaSnaga} kW</td>
                      <td>
                        <span style={{ 
                          backgroundColor: b.status === 'Uparen' ? '#d4edda' : '#fff3cd', 
                          color: b.status === 'Uparen' ? '#155724' : '#856404', 
                          padding: '3px 8px', borderRadius: '4px', fontSize: '14px', fontWeight: 'bold' 
                        }}>
                          {b.status === 'Uparen' ? '🟢 Uparen' : '🟡 Neuparen'}
                        </span>
                      </td>
                      <td>
                        {b.limitVrednost ? (
                          <span style={{ marginRight: '8px' }}>{b.limitVrednost} {b.limitJedinica === 'RSD' ? 'RSD' : 'kWh'}</span>
                        ) : (
                          <span style={{ color: '#6c757d', marginRight: '8px', fontSize: '13px' }}>Nije podešen</span>
                        )}
                        <button onClick={() => otvoriFormuLimita(b)} style={{ backgroundColor: '#fd7e14', color: 'white', border: 'none', padding: '4px 8px', borderRadius: '4px', cursor: 'pointer', fontSize: '12px' }}>
                          {aktivnoBrojiloZaLimit === b.id ? 'Zatvori' : 'Podesi'}
                        </button>

                        {aktivnoBrojiloZaLimit === b.id && (
                          <div style={{ marginTop: '8px', display: 'flex', gap: '5px', alignItems: 'center' }}>
                            <input type="number" step="0.01" placeholder="Vrednost" value={limitVrednost} onChange={e => setLimitVrednost(e.target.value)} style={{ width: '90px', padding: '4px' }} />
                            <select value={limitJedinica} onChange={e => setLimitJedinica(e.target.value)} style={{ padding: '4px' }}>
                              <option value="KWh">kWh</option>
                              <option value="RSD">RSD</option>
                            </select>
                            <button onClick={() => sacuvajLimit(b.id)} style={{ backgroundColor: '#28a745', color: 'white', border: 'none', padding: '4px 8px', borderRadius: '4px', cursor: 'pointer', fontSize: '12px' }}>Sačuvaj</button>
                            {b.limitVrednost && (
                              <button onClick={() => ukloniLimit(b.id)} style={{ backgroundColor: '#dc3545', color: 'white', border: 'none', padding: '4px 8px', borderRadius: '4px', cursor: 'pointer', fontSize: '12px' }}>Ukloni</button>
                            )}
                          </div>
                        )}
                      </td>
                      <td>
                        <button onClick={() => ucitajRacune(b.id)}
                        style = {{ backgroundColor: '#6f42c1', color: 'white', border: 'none', padding: '5px 10px', borderRadius: '4px', cursor: 'pointer', fontSize: '13px'}}>
                          {aktivnoBrojiloZaRacune == b.id ? 'Zatvori racune' : 'Prikazi racune'}
                        </button>
                      </td>
                    </tr>
                    
                    {/* Red sa racunima */}
                    {aktivnoBrojiloZaRacune === b.id && (
                      <tr>
                        <td colSpan="6" style = {{ backgroundColor: '#f8f9fa', padding: '15px'}}>
                          {ucitavamRacune ? (
                            <p>Ucitavam racune...</p>
                          ) : racuni.length === 0 ? (
                            <p style = {{ color: '#6c757d', fontStyle: 'italic'}}>Nema generisanih racuna za ovo brojilo.</p>
                          ) : ( 
                            <table style= {{ width: '100%', borderCollapse: 'collapse', fontSize: '14px'}}>
                              <thead>
                                <tr style={{ backgroundColor: '#e9ecef', textAlign: 'left'}}>
                                  <th style = {{ padding: '8px'}}>Period</th>
                                  <th>VT (kwh)</th>
                                  <th>NT (kwh)</th>
                                  <th>Zelena</th>
                                  <th>Plava</th>
                                  <th>Crvena</th>
                                  <th>Fiksni</th>
                                  <th>Ukupno</th>
                                  <th>Status</th>
                                </tr>
                              </thead>

                              <tbody>
                                {racuni.map(r => (
                                  <tr key = {r.id} style = {{ borderBottom: '1px solid #dee2e6' }}>
                                    <td style = {{ padding: '8px', fontWeight: 'bold'}}>
                                      {String(r.mesecObracuna).padStart(2, '0')}/{r.godinaObracuna}
                                    </td>
                                    <td>{r.energijaVT?.toFixed(2)}</td>
                                    <td>{r.energijaNT?.toFixed(2)}</td>
                                    <td>{r.iznosZelena?.toFixed(2)} RSD</td>
                                    <td>{r.iznosPlava?.toFixed(2)} RSD</td>
                                    <td>{r.iznosCrvena?.toFixed(2)} RSD</td>
                                    <td>{r.fiksniTroskovi?.toFixed(2)} RSD</td>
                                    <td style = {{ fontWeight: 'bold'}}>{r.ukupanIznos?.toFixed(2)} RSD</td>
                                    <td>
                                      <span style = {{ backgroundColor: r.status === 'Placen' ? '#d4edda' : '#f8d7da', color: r.status === 'Placen' ? '#155724' : '#721c24', padding: '3px 8px', borderRadius: '4px', fontWeight: 'bold'}}>
                                        {r.status}
                                      </span>
                                      {r.status !== 'Placen' && (
                                        <button onClick={() => platiRacun(r.id)} style={{ marginLeft: '8px', backgroundColor: '#28a745', color: 'white', border: 'none', padding: '3px 10px', borderRadius: '4px', cursor: 'pointer', fontSize: '12px' }}>
                                          Plati
                                        </button>
                                      )}
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          )}
                        </td>
                      </tr>
                    )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            ) : (
              <p style={{ color: '#6c757d', fontStyle: 'italic' }}>Nema dodatih brojila za ovaj objekat.</p>
            )}

            {/* Dugme za otvaranje forme za dodavanje brojila */}
            {aktivniObjekatId !== obj.id ? (
              <button 
                onClick={() => setAktivniObjekatId(obj.id)} 
                style={{ backgroundColor: '#17a2b8', color: 'white', border: 'none', padding: '8px 15px', borderRadius: '4px', cursor: 'pointer' }}
              >
                + Registruj brojilo
              </button>
            ) : (
              /* Forma za dodavanje brojila - OznakaBrojila je uspešno izbačena */
              <div style={{ marginTop: '15px', padding: '15px', backgroundColor: '#f8f9fa', borderRadius: '4px', border: '1px dashed #ced4da' }}>
                <h5 style={{ marginTop: 0 }}>Registracija novog brojila (Format: SA-YYYY-XXXXX)</h5>
                <form onSubmit={(e) => handleRegistrujBrojilo(e, obj.id)} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
                  
                  <input type="text" name="serijskiBroj" placeholder="Serijski broj (npr. SA-2026-12345)" required style={{ padding: '8px', gridColumn: 'span 2' }} />
                  
                  <select name="tip" defaultValue="Monofazni" style={{ padding: '8px' }}>
                    <option value="Monofazni">Monofazni priključak (6.9 kW)</option>
                    <option value="Trofazni">Trofazni priključak (11.04 kW)</option>
                  </select>
                  
                  <input type="text" name="napomena" placeholder="Napomena (opciono)" style={{ padding: '8px' }} />

                  <div style={{ gridColumn: 'span 2', display: 'flex', gap: '10px' }}>
                    <button type="submit" style={{ backgroundColor: '#28a745', color: 'white', border: 'none', padding: '8px 15px', borderRadius: '4px', cursor: 'pointer' }}>Registruj</button>
                    <button type="button" onClick={() => setAktivniObjekatId(null)} style={{ backgroundColor: '#dc3545', color: 'white', border: 'none', padding: '8px 15px', borderRadius: '4px', cursor: 'pointer' }}>Odustani</button>
                  </div>
                </form>
              </div>
            )}
          </div>
        ))}
        {objekti.length === 0 && (
          <p style={{ textAlign: 'center', color: '#6c757d', padding: '20px' }}>Trenutno nemate nijedan unet objekat.</p>
        )}
      </div>
    </div>
  );
};

export default PotrosacDashboard;
