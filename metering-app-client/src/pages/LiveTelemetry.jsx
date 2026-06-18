import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, LineChart, Line } from 'recharts';

const inicijalniPodaciPotrosnje = [
  { dan: 'Danas', VT: 0, NT: 0 }, 
];

const LiveTelemetry = () => {
  const { brojiloId } = useParams(); 
  const navigate = useNavigate();

  const [poslednjeMerenje, setPoslednjeMerenje] = useState(null);
  const [statusKonekcije, setStatusKonekcije] = useState('Povezivanje...');
  const [greska, setGreska] = useState('');

  // Dinamičko stanje za tip brojila koje se određuje na osnovu strukture paketa
  const [tipBrojila, setTipBrojila] = useState(null); 

  const [trendPodaci, setTrendPodaci] = useState([]); 
  const [dnevnaPotrosnja, setDnevnaPotrosnja] = useState(inicijalniPodaciPotrosnje); 

  const prethodnaPotrosnjaRef = useRef(null); 
  const konekcijaRef = useRef(null);

  // POMOĆNA FUNKCIJA za bezbedno čitanje propertija
  const getProp = (obj, propName) => {
    if (!obj) return null;
    const vrednost = obj[propName] !== undefined ? obj[propName] : obj[propName.charAt(0).toLowerCase() + propName.slice(1)];
    if (vrednost === null || vrednost === "null" || vrednost === undefined) return null;
    return vrednost;
  };

  useEffect(() => {
    let active = true;

    setPoslednjeMerenje(null);
    setTipBrojila(null); // Resetujemo tip prilikom promene brojila
    setTrendPodaci([]);
    setDnevnaPotrosnja(inicijalniPodaciPotrosnje); 
    setStatusKonekcije('Povezivanje...');
    prethodnaPotrosnjaRef.current = null;
    
    const novaKonekcija = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:7056/api') 
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning) 
      .build();

    konekcijaRef.current = novaKonekcija;

    novaKonekcija.on('NovoMerenjeStiglo', (telemetrija) => {
      if (!active) return;
      
      const siroviId = telemetrija.BrojiloId || telemetrija.brojiloId || telemetrija.idBrojila;
      if (siroviId && String(siroviId).toLowerCase() === String(brojiloId).toLowerCase()) {
        
        setPoslednjeMerenje(telemetrija);

        // DETEKCIJA NA OSNOVU PODATAKA: Gledamo da li u paketu stvarno postoje fazni naponi
        const imaFazu1 = getProp(telemetrija, 'NaponL1') !== null;
        const detektovaniTip = imaFazu1 ? 'trofazno' : 'monofazno';
        
        setTipBrojila(detektovaniTip);

        setTrendPodaci((prethodni) => {
          const vremeLokalno = new Date(telemetrija.VremeMerenja || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
          const opterecenje = telemetrija.TrenutnoOpterecenje ?? telemetrija.trenutnoOpterecenje ?? 0;

          let tackaGrafikona = {
            vreme: vremeLokalno,
            opterecenje: opterecenje
          };

          // Punimo grafikon na osnovu detektovanog tipa iz ovog paketa
          if (detektovaniTip === 'trofazno') {
            tackaGrafikona.naponL1 = getProp(telemetrija, 'NaponL1') ?? 230;
            tackaGrafikona.naponL2 = getProp(telemetrija, 'NaponL2') ?? 230;
            tackaGrafikona.naponL3 = getProp(telemetrija, 'NaponL3') ?? 230;
          } else {
            tackaGrafikona.napon = getProp(telemetrija, 'Napon') ?? 230;
          }

          const azurirano = [...prethodni, tackaGrafikona];
          if (azurirano.length > 10) azurirano.shift();
          return azurirano;
        });

        const trenutnaUkupnaPotrosnja = Number(telemetrija.UkupnaPotrosnja ?? telemetrija.ukupnaPotrosnja ?? 0);
        const tarifa = telemetrija.Tarifa ?? telemetrija.tarifa;

        if (prethodnaPotrosnjaRef.current === null) {
          prethodnaPotrosnjaRef.current = trenutnaUkupnaPotrosnja;
          return; 
        }

        const potroseno = trenutnaUkupnaPotrosnja - prethodnaPotrosnjaRef.current;
        prethodnaPotrosnjaRef.current = trenutnaUkupnaPotrosnja;

        if (potroseno <= 0) return;

        setDnevnaPotrosnja((prethodniNiz) => {
          return prethodniNiz.map((stavka) => {
            if (stavka.dan === 'Danas') {
              if (tarifa === 1 || tarifa === "VisaTarifa") {
                return { ...stavka, VT: Number((stavka.VT + potroseno).toFixed(2)) };
              } else {
                return { ...stavka, NT: Number((stavka.NT + potroseno).toFixed(2)) };
              }
            }
            return stavka;
          });
        });
      }
    });

    const pokreniVezu = async () => {
      try {
        await novaKonekcija.start();
        if (!active) return;
        setStatusKonekcije('Povezan');
        await fetch('http://localhost:7056/api/joinGroup', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ connectionId: novaKonekcija.connectionId, groupName: brojiloId })
        });
      } catch (err) {
        if (!active) return;
        setStatusKonekcije('Greška');
      }
    };

    if (brojiloId) pokreniVezu();

    return () => {
      active = false;
      if (novaKonekcija) {
        novaKonekcija.off('NovoMerenjeStiglo');
        novaKonekcija.stop().catch(() => {});
      }
    };
  }, [brojiloId]);

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', backgroundColor: '#f4f6f9', minHeight: '100vh' }}>
      
      {/* ZAGLAVLJE */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <button onClick={() => navigate(-1)} style={{ padding: '8px 16px', cursor: 'pointer', background: '#6c757d', color: 'white', border: 'none', borderRadius: '4px', fontWeight: 'bold' }}>
          ⬅ Nazad na Brojila
        </button>
        <span style={{ fontSize: '14px', color: '#666' }}>
          Status veze: <strong style={{ color: statusKonekcije === 'Povezan' ? '#28a745' : '#dc3545' }}>{statusKonekcije}</strong>
        </span>
      </div>

      <h2>
        Telemetrijski Panel: {' '}
        <span style={{ color: tipBrojila === 'trofazno' ? '#e63946' : tipBrojila === 'monofazno' ? '#007bff' : '#6c757d' }}>
          {tipBrojila === 'trofazno' ? '⚡ Trofazno Industrijsko' : tipBrojila === 'monofazno' ? 'Monofazno Kućno' : 'Očitavanje tipa...'}
        </span>
      </h2>
      <p style={{ color: '#6c757d', marginBottom: '25px' }}>Uređaj ID: <strong style={{ fontFamily: 'monospace' }}>{brojiloId}</strong></p>

      {/* KARTICE - ZAJEDNIČKE ZA OBA MODELA */}
      <div style={{ display: 'flex', gap: '15px', marginBottom: '25px' }}>
        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Aktivna Tarifa</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#fd7e14' }}>
            {poslednjeMerenje ? (getProp(poslednjeMerenje, 'Tarifa') === 1 || getProp(poslednjeMerenje, 'Tarifa') === "VisaTarifa" ? 'Viša (VT)' : 'Niža (NT)') : 'Čeka se signal...'}
          </p>
        </div>

        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Ukupno Stanje</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#28a745' }}>
            {poslednjeMerenje ? `${Number(getProp(poslednjeMerenje, 'UkupnaPotrosnja')).toFixed(2)} kWh` : '0.00 kWh'}
          </p>
        </div>

        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Trenutno Opterećenje</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#6f42c1' }}>
            {poslednjeMerenje ? `${Number(getProp(poslednjeMerenje, 'TrenutnoOpterecenje')).toFixed(2)} kW` : '0.00 kW'}
          </p>
        </div>
      </div>

      {/* ========================================================================= */}
      {/* DINAMIČKI EKRAN ZASNOVAN NA DATOTEKAMA KOJE STIŽU SA SIMULATORA*/}
      {/* ========================================================================= */}
      
      {!tipBrojila ? (
        <div style={{ padding: '40px', textAlign: 'center', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #dee2e6', color: '#6c757d', fontStyle: 'italic' }}>
          Čeka se prvi telemetrijski paket sa simulatora kako bi se prepoznala arhitektura brojila...
        </div>
      ) : tipBrojila === 'trofazno' ? (
        /* RENDEROVANJE: TROFAZNI GRAFIKONI I TABELA */
        <div style={{ display: 'flex', flexDirection: 'column', gap: '25px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
              <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '15px' }}>Akumulacija Potrošnje (Danas)</h3>
              <div style={{ width: '100%', height: 260, minWidth: 0 }}>
                <ResponsiveContainer>
                  <BarChart data={dnevnaPotrosnja} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="dan" />
                    <YAxis unit=" kWh" />
                    <Tooltip />
                    <Bar dataKey="VT" name="Viša Tarifa (VT)" fill="#fd7e14" />
                    <Bar dataKey="NT" name="Niža Tarifa (NT)" fill="#17a2b8" />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
              <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '15px' }}>Trofazni Trend Napona po Fazama L1, L2, L3 (Uživo)</h3>
              <div style={{ width: '100%', height: 260, minWidth: 0 }}>
                <ResponsiveContainer>
                  <LineChart data={trendPodaci} margin={{ top: 10, right: 5, left: -20, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="vreme" />
                    <YAxis domain={[150, 260]} unit=" V" />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="naponL1" name="Faza L1 (V)" stroke="#e63946" strokeWidth={2.5} dot={false} isAnimationActive={false} />
                    <Line type="monotone" dataKey="naponL2" name="Faza L2 (V)" stroke="#ffb703" strokeWidth={2.5} dot={false} isAnimationActive={false} />
                    <Line type="monotone" dataKey="naponL3" name="Faza L3 (V)" stroke="#00b4d8" strokeWidth={2.5} dot={false} isAnimationActive={false} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          <div style={{ padding: '20px', borderRadius: '8px', backgroundColor: 'white', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
            <h4 style={{ marginTop: 0, color: '#2c3e50', borderBottom: '1px solid #eee', paddingBottom: '10px' }}>Detaljna Analiza po Fazama</h4>
            <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '10px', textAlign: 'left' }}>
              <thead>
                <tr style={{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                  <th style={{ padding: '10px' }}>Oznaka Faze</th>
                  <th>Napon (V)</th>
                  <th>Struja (A)</th>
                  <th>Faktor Snage (cos φ)</th>
                </tr>
              </thead>
              <tbody>
                <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#e63946' }}>🔴 Linija L1</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL1')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL1')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL1')}</td>
                </tr>
                <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#ffb703' }}>🟡 Linija L2</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL2')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL2')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL2')}</td>
                </tr>
                <tr>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#00b4d8' }}>🔵 Linija L3</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL3')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL3')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL3')}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        /* RENDEROVANJE: MONOFAZNI ČISTI GRAFIKONI I BLOKOVI */
        <div style={{ display: 'flex', flexDirection: 'column', gap: '25px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
              <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '15px' }}>Potrošnja Objekta (Danas)</h3>
              <div style={{ width: '100%', height: 260, minWidth: 0 }}>
                <ResponsiveContainer>
                  <BarChart data={dnevnaPotrosnja} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="dan" />
                    <YAxis unit=" kWh" />
                    <Tooltip />
                    <Bar dataKey="VT" name="Viša Tarifa (VT)" fill="#fd7e14" />
                    <Bar dataKey="NT" name="Niža Tarifa (NT)" fill="#17a2b8" />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
              <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '15px' }}>Kriva Napona i Potrošnje Snage (Uživo)</h3>
              <div style={{ width: '100%', height: 260, minWidth: 0 }}>
                <ResponsiveContainer>
                  <LineChart data={trendPodaci} margin={{ top: 10, right: 5, left: -15, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="vreme" />
                    <YAxis yAxisId="levo" orientation="left" domain={[150, 260]} unit=" V" stroke="#007bff" />
                    <YAxis yAxisId="desno" orientation="right" unit=" kW" stroke="#6f42c1" />
                    <Tooltip />
                    <Legend />
                    <Line yAxisId="levo" type="monotone" dataKey="napon" name="Napon Mreže (V)" stroke="#007bff" strokeWidth={2.5} dot={{ r: 3 }} isAnimationActive={false} />
                    <Line yAxisId="desno" type="monotone" dataKey="opterecenje" name="Potrošnja (kW)" stroke="#6f42c1" strokeWidth={2.5} dot={{ r: 3 }} isAnimationActive={false} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          <div style={{ padding: '20px', borderRadius: '8px', backgroundColor: 'white', border: '1px solid #dee2e6', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
            <h4 style={{ marginTop: 0, color: '#2c3e50', borderBottom: '1px solid #eee', paddingBottom: '10px' }}>Mrežni Status Jednofaznog Priključka</h4>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '20px', marginTop: '15px' }}>
              <div style={{ backgroundColor: '#f8f9fa', padding: '15px', borderRadius: '6px', border: '1px solid #e9ecef' }}>
                <span style={{ color: '#666', fontSize: '13px', display: 'block', marginBottom: '5px' }}>Izmereni Napon:</span>
                <strong style={{ fontSize: '18px', color: '#007bff' }}>{getProp(poslednjeMerenje, 'Napon') ?? 230} V</strong>
              </div>
              <div style={{ backgroundColor: '#f8f9fa', padding: '15px', borderRadius: '6px', border: '1px solid #e9ecef' }}>
                <span style={{ color: '#666', fontSize: '13px', display: 'block', marginBottom: '5px' }}>Jačina Struje:</span>
                <strong style={{ fontSize: '18px', color: '#333' }}>{getProp(poslednjeMerenje, 'Struja') ?? 0} A</strong>
              </div>
              <div style={{ backgroundColor: '#f8f9fa', padding: '15px', borderRadius: '6px', border: '1px solid #e9ecef' }}>
                <span style={{ color: '#666', fontSize: '13px', display: 'block', marginBottom: '5px' }}>Faktor Snage (cos φ):</span>
                <strong style={{ fontSize: '18px', color: '#333' }}>{getProp(poslednjeMerenje, 'FaktorSnage') ?? 1.0}</strong>
              </div>
            </div>
          </div>
        </div>
      )}

    </div>
  );
};

export default LiveTelemetry;