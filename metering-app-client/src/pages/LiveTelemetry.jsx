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

  const [trendPodaci, setTrendPodaci] = useState([]); 
  const [dnevnaPotrosnja, setDnevnaPotrosnja] = useState(inicijalniPodaciPotrosnje); 

  const prethodnaPotrosnjaRef = useRef(null); 
  const konekcijaRef = useRef(null);

  //POMOĆNA FUNKCIJA za bezbedno čitanje propertija
  const getProp = (obj, propName) => {
    if (!obj) return null;
    return obj[propName] !== undefined ? obj[propName] : obj[propName.charAt(0).toLowerCase() + propName.slice(1)];
  };

  // DETEKCIJA DA LI JE BROJILO TROFAZNO
  const jeTrofazno = poslednjeMerenje && (
    getProp(poslednjeMerenje, 'NaponL1') !== null || 
    getProp(poslednjeMerenje, 'naponL1') !== null
  );

  useEffect(() => {
    let active = true;

    setPoslednjeMerenje(null);
    setTrendPodaci([]);
    setDnevnaPotrosnja(inicijalniPodaciPotrosnje); 
    setStatusKonekcije('Povezivanje...');
    prethodnaPotrosnjaRef.current = null;

    console.log("Inicijalizacija SignalR veze za brojilo:", brojiloId);
    
    const novaKonekcija = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:7056/api') 
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning) 
      .build();

    konekcijaRef.current = novaKonekcija;

    novaKonekcija.on('NovoMerenjeStiglo', (telemetrija) => {
      if (!active) return;
      
      const siroviId = telemetrija.BrojiloId || telemetrija.brojiloId || telemetrija.idBrojila;
      const dolazniIdString = siroviId ? String(siroviId).toLowerCase() : "";
      const trenutniIdString = brojiloId ? String(brojiloId).toLowerCase() : "";

      if (dolazniIdString && dolazniIdString === trenutniIdString) {
        
        setPoslednjeMerenje(telemetrija);

        // PUNJENJE LINIJSKOG GRAFIKONA U ZAVISNOSTI OD TIP BROJILA
        setTrendPodaci((prethodni) => {
          const vremeLokalno = new Date(telemetrija.VremeMerenja || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
          const opterecenje = telemetrija.TrenutnoOpterecenje ?? telemetrija.trenutnoOpterecenje ?? 0;

          let tackaGrafikona = {
            vreme: vremeLokalno,
            opterecenje: opterecenje
          };

          // Ako je trofazno, izvlačimo sva tri napona zasebno
          if (getProp(telemetrija, 'NaponL1') !== null) {
            tackaGrafikona.naponL1 = getProp(telemetrija, 'NaponL1');
            tackaGrafikona.naponL2 = getProp(telemetrija, 'NaponL2');
            tackaGrafikona.naponL3 = getProp(telemetrija, 'NaponL3');
          } else {
            // Ako je monofazno, punimo samo standardni napon
            tackaGrafikona.napon = getProp(telemetrija, 'Napon') ?? 230;
          }

          const azurirano = [...prethodni, tackaGrafikona];
          if (azurirano.length > 10) azurirano.shift();
          return azurirano;
        });

        // PUNJENJE STUBIČASTOG GRAFIKONA (Pametni prirast)
        const trenutnaUkupnaPotrosnja = Number(telemetrija.UkupnaPotrosnja ?? telemetrija.ukupnaPotrosnja ?? 0);
        const tarifa = telemetrija.Tarifa ?? telemetrija.tarifa;

        if (prethodnaPotrosnjaRef.current === null) {
          prethodnaPotrosnjaRef.current = trenutnaUkupnaPotrosnja;
          return; 
        }

        const potrosenoUTomTrenutku = trenutnaUkupnaPotrosnja - prethodnaPotrosnjaRef.current;
        prethodnaPotrosnjaRef.current = trenutnaUkupnaPotrosnja;

        if (potrosenoUTomTrenutku <= 0) return;

        setDnevnaPotrosnja((prethodniNiz) => {
          return prethodniNiz.map((stavka) => {
            if (stavka.dan === 'Danas') {
              if (tarifa === 1 || tarifa === "VisaTarifa") {
                return { ...stavka, VT: Number((stavka.VT + potrosenoUTomTrenutku).toFixed(2)) };
              } else {
                return { ...stavka, NT: Number((stavka.NT + potrosenoUTomTrenutku).toFixed(2)) };
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
        if (!active) {
          await novaKonekcija.stop();
          return;
        }
        setStatusKonekcije('Povezan');
        
        await fetch('http://localhost:7056/api/joinGroup', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ 
            connectionId: novaKonekcija.connectionId,
            groupName: brojiloId 
          })
        });
        console.log('Uspešno dodat u SignalR grupu na bekend-u.');
      } catch (err) {
        if (!active) return;
        setStatusKonekcije('Greška pri konekciji');
        setGreska('Sistem nije uspeo da se poveže na SignalR servis.');
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
        <button 
          onClick={() => navigate(-1)} 
          style={{ padding: '8px 16px', cursor: 'pointer', background: '#6c757d', color: 'white', border: 'none', borderRadius: '4px', fontWeight: 'bold' }}
        >
          ⬅ Nazad na Brojila
        </button>
        <span style={{ fontSize: '14px', color: '#666' }}>
          Status veze: <strong style={{ color: statusKonekcije === 'Povezan' ? '#28a745' : '#dc3545' }}>{statusKonekcije}</strong>
        </span>
      </div>

      <h2>Telemetrijski Podaci Uživo {jeTrofazno ? ' (Trofazno Brojilo)' : ' (Monofazno Brojilo)'}</h2>
      <p style={{ color: '#6c757d', marginBottom: '20px' }}>
        GUID uređaja: <strong style={{ fontFamily: 'monospace' }}>{brojiloId}</strong>
      </p>

      {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}

      {/* 📇 1. KARTICE SA DINAMIČKIM TROFAZNIM PRIKAZOM */}
      <div style={{ display: 'flex', gap: '15px', marginBottom: '25px' }}>
        
        {/* TARIFA */}
        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Trenutna Tarifa</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#fd7e14' }}>
            {poslednjeMerenje ? (getProp(poslednjeMerenje, 'Tarifa') === 1 || getProp(poslednjeMerenje, 'Tarifa') === "VisaTarifa" ? 'Viša (VT)' : 'Niža (NT)') : 'Čeka se signal...'}
          </p>
        </div>

        {/* UKUPNA POTROŠNJA */}
        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Ukupna Potrošnja</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#28a745' }}>
            {poslednjeMerenje ? `${Number(getProp(poslednjeMerenje, 'UkupnaPotrosnja')).toFixed(2)} kWh` : '0.00 kWh'}
          </p>
        </div>

        {/* NAPON KARTICA - DINAMIČKA */}
        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Mrežni Napon</h4>
          {poslednjeMerenje ? (
            jeTrofazno ? (
              <div style={{ fontSize: '15px', fontWeight: 'bold', display: 'flex', flexDirection: 'column', gap: '3px' }}>
                <span style={{ color: '#e63946' }}>🔴 L1: {getProp(poslednjeMerenje, 'NaponL1')} V</span>
                <span style={{ color: '#ffb703' }}>🟡 L2: {getProp(poslednjeMerenje, 'NaponL2')} V</span>
                <span style={{ color: '#00b4d8' }}>🔵 L3: {getProp(poslednjeMerenje, 'NaponL3')} V</span>
              </div>
            ) : (
              <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#007bff' }}>
                {getProp(poslednjeMerenje, 'Napon') ?? 230} V
              </p>
            )
          ) : <p style={{ margin: 0, fontWeight: 'bold', color: '#ccc' }}>Čeka se signal...</p>}
        </div>

        {/* OPTEREĆENJE KARTICA */}
        <div style={{ padding: '20px', borderRadius: '8px', flex: 1, backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '13px', textTransform: 'uppercase' }}>Trenutna Snaga</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#6f42c1' }}>
            {poslednjeMerenje ? `${Number(getProp(poslednjeMerenje, 'TrenutnoOpterecenje')).toFixed(2)} kW` : '0.00 kW'}
          </p>
        </div>

      </div>

      {/* 📊 2. SREDIŠNJI DEO SA GRAFIKONIMA */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '25px' }}>
        
        {/* STUBIČASTI GRAFIKON */}
        <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '16px' }}>Današnja Potrošnja po Tarifama (Uživo)</h3>
          <div style={{ width: '100%', height: 280 }}>
            <ResponsiveContainer>
              <BarChart data={dnevnaPotrosnja} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="dan" />
                <YAxis unit=" kWh" />
                <Tooltip />
                <Legend />
                <Bar dataKey="VT" name="Viša Tarifa (VT)" fill="#fd7e14" />
                <Bar dataKey="NT" name="Niža Tarifa (NT)" fill="#17a2b8" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* LINIJSKI GRAFIKON - DINAMIČKI CRTA 1 ILI 3 LINIJE ZA NAPON */}
        <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
          <h3 style={{ marginTop: 0, color: '#2c3e50', fontSize: '16px' }}>Trend Napona i Opterećenja (Uživo)</h3>
          <div style={{ width: '100%', height: 280 }}>
            <ResponsiveContainer>
              <LineChart data={trendPodaci} margin={{ top: 10, right: 5, left: 5, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="vreme" />
                <YAxis yAxisId="levo" orientation="left" domain={[210, 250]} unit=" V" stroke="#007bff" />
                <YAxis yAxisId="desno" orientation="right" domain={[0, 'auto']} unit=" kW" stroke="#6f42c1" />
                <Tooltip />
                <Legend />
                
                {/* AKO JE TROFAZNO, CRTAMO TRI LINIJE ZA NAPON, AKO JE JEDNOFAZNO SAMO JEDNU */}
                {jeTrofazno ? (
                  <>
                    <Line yAxisId="levo" type="monotone" dataKey="naponL1" name="Napon L1 (V)" stroke="#e63946" strokeWidth={2} dot={false} isAnimationActive={false} />
                    <Line yAxisId="levo" type="monotone" dataKey="naponL2" name="Napon L2 (V)" stroke="#ffb703" strokeWidth={2} dot={false} isAnimationActive={false} />
                    <Line yAxisId="levo" type="monotone" dataKey="naponL3" name="Napon L3 (V)" stroke="#00b4d8" strokeWidth={2} dot={false} isAnimationActive={false} />
                  </>
                ) : (
                  <Line yAxisId="levo" type="monotone" dataKey="napon" name="Napon (V)" stroke="#007bff" strokeWidth={2.5} dot={{ r: 2 }} isAnimationActive={false} />
                )}

                {/* Zajednička linija opterećenja na desnoj osi */}
                <Line yAxisId="desno" type="monotone" dataKey="opterecenje" name="Opterećenje (kW)" stroke="#6f42c1" strokeWidth={2.5} dot={{ r: 2 }} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

      </div>

      {/*DETALJNI PRIKAZ SVIH PROPERTIJA SA BEKENDA */}
      <div style={{ padding: '20px', borderRadius: '8px', backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', border: '1px solid #dee2e6' }}>
        <h4 style={{ marginTop: 0, color: '#2c3e50' }}>Kompletni Mrežni Parametri sa Brojila</h4>
        {poslednjeMerenje ? (
          jeTrofazno ? (
            /* TROFAZNA TABELA */
            <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '15px', textAlign: 'left' }}>
              <thead>
                <tr style={{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                  <th style={{ padding: '10px' }}>Faza</th>
                  <th>Napon (V)</th>
                  <th>Struja (A)</th>
                  <th>Faktor snage (cos φ)</th>
                </tr>
              </thead>
              <tbody>
                <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#e63946' }}>🔴 Faza L1</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL1')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL1')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL1')}</td>
                </tr>
                <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#ffb703' }}>🟡 Faza L2</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL2')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL2')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL2')}</td>
                </tr>
                <tr style={{ borderBottom: '1px solid #dee2e6' }}>
                  <td style={{ padding: '10px', fontWeight: 'bold', color: '#00b4d8' }}>🔵 Faza L3</td>
                  <td>{getProp(poslednjeMerenje, 'NaponL3')} V</td>
                  <td>{getProp(poslednjeMerenje, 'StrujaL3')} A</td>
                  <td>{getProp(poslednjeMerenje, 'FaktorSnageL3')}</td>
                </tr>
              </tbody>
            </table>
          ) : (
            /* JEDNOFAZNI PREGLED */
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '15px', marginTop: '15px' }}>
              <div>
                <p style={{ margin: '0 0 5px 0', color: '#666' }}>Mrežni Napon:</p>
                <strong>{getProp(poslednjeMerenje, 'Napon') ?? 230} V</strong>
              </div>
              <div>
                <p style={{ margin: '0 0 5px 0', color: '#666' }}>Jačina struje:</p>
                <strong>{getProp(poslednjeMerenje, 'Struja') ?? 0} A</strong>
              </div>
              <div>
                <p style={{ margin: '0 0 5px 0', color: '#666' }}>Faktor snage (cos φ):</p>
                <strong>{getProp(poslednjeMerenje, 'FaktorSnage') ?? 1.0}</strong>
              </div>
            </div>
          )
        ) : (
          <p style={{ color: '#6c757d', margin: 0, fontStyle: 'italic' }}>Čeka se prvi paket sa simulatora...</p>
        )}
      </div>

    </div>
  );
};

export default LiveTelemetry;