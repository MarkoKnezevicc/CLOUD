import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';

const LiveTelemetry = () => {
  const { brojiloId } = useParams(); 
  const navigate = useNavigate();

  const [poslednjeMerenje, setPoslednjeMerenje] = useState(null);
  const [statusKonekcije, setStatusKonekcije] = useState('Povezivanje...');
  const [greska, setGreska] = useState('');

  const konekcijaRef = useRef(null);

  useEffect(() => {
    let active = true;

    console.log("Inicijalizacija SignalR veze za brojilo:", brojiloId);
    //konfiguracija veze
    const novaKonekcija = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:7056/api') 
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning) 
      .build();

    konekcijaRef.current = novaKonekcija;

    // Slušamo događaj sa bekenda
    novaKonekcija.on('NovoMerenjeStiglo', (telemetrija) => {
  if (!active) return;
  
  console.log("SignalR poruka je fizički stigla u React! Sadržaj:", telemetrija);

  // 1. Izvlačimo ID defanzivno
  const siroviId = telemetrija.BrojiloId || telemetrija.brojiloId || telemetrija.idBrojila;

  // 2. 🔑 POPRAVAK: Pretvaramo ga u string sigurno pre poređenja
  const dolazniIdString = siroviId ? String(siroviId).toLowerCase() : "";
  const trenutniIdString = brojiloId ? String(brojiloId).toLowerCase() : "";

  // 3. Poredimo čiste tekstualne vrednosti
  if (dolazniIdString && dolazniIdString === trenutniIdString) {
    console.log('ID se poklapa! Ažuriram stanje na ekranu.');
    setPoslednjeMerenje(telemetrija);
  } else {
    console.log(`ID se NE poklapa. Traženo: ${trenutniIdString}, Dobijeno: ${dolazniIdString}`);
  }
});
    //samo pokretanje(iniciranje komunikacije)
    const pokreniVezu = async () => {
      try {
        await novaKonekcija.start();
        
        if (!active) {
          await novaKonekcija.stop();
          return;
        }

        setStatusKonekcije('Povezan');
        console.log(`🟢WebSocket otvoren. Registrujem connectionId: ${novaKonekcija.connectionId} u grupu: ${brojiloId}`);
        
        //Šaljemo tačne nazive ključeva koje JoinGroupDto očekuje
        const response = await fetch('http://localhost:7056/api/joinGroup', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ 
            connectionId: novaKonekcija.connectionId,
            groupName: brojiloId // Prosleđujemo GUID brojila kao naziv grupe/sobe
          })
        });

        if (!response.ok) {
          throw new Error('Neuspešna registracija u grupu preko API endpoints.');
        }

        console.log('Uspešno dodat u SignalR grupu na bekend-u.');

      } catch (err) {
        if (!active) return;
        setStatusKonekcije('Greška pri konekciji');
        setGreska('Sistem nije uspeo da se poveže na SignalR servis.');
        console.error('SignalR greška:', err);
      }
    };

    if (brojiloId) {
      pokreniVezu();
    }

    return () => {
      active = false;
      if (novaKonekcija) {
        novaKonekcija.off('NovoMerenjeStiglo');
        novaKonekcija.stop().catch(() => {});
      }
    };
  }, [brojiloId]);

  // Pomoćna funkcija za bezbedno čitanje propertija (bilo veliko ili malo slovo)
  const getProp = (obj, propName) => {
    if (!obj) return null;
    return obj[propName] !== undefined ? obj[propName] : obj[propName.charAt(0).toLowerCase() + propName.slice(1)];
  };

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
      
      {/* ZAGLAVLJE */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <button 
          onClick={() => navigate(-1)} 
          style={{ padding: '8px 16px', cursor: 'pointer', background: '#6c757d', color: 'white', border: 'none', borderRadius: '4px', fontWeight: 'bold' }}
        >
          Nazad na Brojila
        </button>
        <span style={{ fontSize: '14px', color: '#666' }}>
          Status veze: <strong style={{ color: statusKonekcije === 'Povezan' ? '#28a745' : '#dc3545' }}>{statusKonekcije}</strong>
        </span>
      </div>

      <h2>Telemetrijski Podaci u Realnom Vremenu</h2>
      <p style={{ color: '#6c757d', marginBottom: '20px' }}>
        Pratite uređaj (GUID): <strong style={{ fontFamily: 'monospace' }}>{brojiloId}</strong>
      </p>

      {greska && <div style={{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}

      {/* KARTICE SA PODACIMA */}
      <div style={{ display: 'flex', gap: '15px', marginBottom: '20px' }}>
        
        <div style={{ padding: '20px', border: '1px solid #dee2e6', borderRadius: '8px', flex: 1, backgroundColor: '#f8f9fa' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '14px', textTransform: 'uppercase' }}>Trenutna Tarifa</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#fd7e14' }}>
            {poslednjeMerenje ? (getProp(poslednjeMerenje, 'Tarifa') === 1 || getProp(poslednjeMerenje, 'Tarifa') === "VisaTarifa" ? 'Viša (VT)' : 'Niža (NT)') : 'Čekanje signala...'}
          </p>
        </div>

        <div style={{ padding: '20px', border: '1px solid #dee2e6', borderRadius: '8px', flex: 1, backgroundColor: '#f8f9fa' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '14px', textTransform: 'uppercase' }}>Ukupna Potrošnja</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#28a745' }}>
            {poslednjeMerenje ? `${getProp(poslednjeMerenje, 'UkupnaPotrosnja')} kWh` : '0.00 kWh'}
          </p>
        </div>

        <div style={{ padding: '20px', border: '1px solid #dee2e6', borderRadius: '8px', flex: 1, backgroundColor: '#f8f9fa' }}>
          <h4 style={{ margin: '0 0 10px 0', color: '#6c757d', fontSize: '14px', textTransform: 'uppercase' }}>Trenutno Opterećenje</h4>
          <p style={{ fontSize: '24px', margin: 0, fontWeight: 'bold', color: '#007bff' }}>
            {poslednjeMerenje ? `${getProp(poslednjeMerenje, 'TrenutnoOpterecenje')} kW` : '0.00 kW'}
          </p>
        </div>

      </div>

      {/* DETALJNI PRIKAZ NAPONA I STRUJE */}
      <div style={{ padding: '20px', border: '1px solid #dee2e6', borderRadius: '8px', backgroundColor: 'white', boxShadow: '0 2px 4px rgba(0,0,0,0.02)' }}>
        <h4 style={{ marginTop: 0, color: '#2c3e50' }}>Mrežni Parametri (Zadnje merenje)</h4>
        {poslednjeMerenje ? (
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
        ) : (
          <p style={{ color: '#6c757d', margin: 0, fontStyle: 'italic' }}>
            Uključite simulator kako biste poslali podatke sa X-Device-Tokenom ovog brojila.
          </p>
        )}
      </div>

    </div>
  );
};

export default LiveTelemetry;