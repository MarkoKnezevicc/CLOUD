import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

const GlobalNotificationCenter = () => {
  const [alarmi, setAlarmi] = useState([]);
  const [konekcijaStatus, setKonekcijaStatus] = useState('Povezivanje...');
  const konekcijaRef = useRef(null);

  useEffect(() => {
    let active = true;

    console.log("📡 Pokrećem GLOBALNI centar za hitna upozorenja...");

    const globalnaKonekcija = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:7056/api') 
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning) 
      .build();

    konekcijaRef.current = globalnaKonekcija;

    // SLUŠAMO GLOBALNE EVENTE IZ GRUPE "SvaKriticnaStanja"
    globalnaKonekcija.on('KriticanNaponUpozorenje', (alarm) => {
      if (!active || !alarm) return;
      
      console.log("💥 STIGAO GLOBALNI ALARM U LOGOVE:", alarm);

      const siroviId = alarm.BrojiloId || alarm.brojiloId || "";
      const idKaoString = String(siroviId);

      const noviAlarm = {
        id: Math.random().toString(), 
        poruka: alarm.Poruka || alarm.poruka || "Kritičan pad napona!",
        brojiloId: idKaoString, 
        vreme: new Date(alarm.Vreme || alarm.vreme || Date.now()).toLocaleTimeString() 
    };

      setAlarmi((prethodni) => [noviAlarm, ...prethodni]);
    });

    const pokreniGlobalnuVezu = async () => {
      try {
        await globalnaKonekcija.start();
        if (!active) return;

        setKonekcijaStatus('Povezan');
        console.log(`📡 Globalni mrežni ID: ${globalnaKonekcija.connectionId}. Ulazak u sobu SvaKriticnaStanja`);

        await fetch('http://localhost:7056/api/joinGroup', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ 
            connectionId: globalnaKonekcija.connectionId, 
            groupName: 'SvaKriticnaStanja' 
          })
        });

      } catch (err) {
        if (!active) return;
        setKonekcijaStatus('Greška');
        console.error('Greška pri podizanju globalnog centra:', err);
      }
    };

    pokreniGlobalnuVezu();

    return () => {
      active = false;
      if (globalnaKonekcija) {
        globalnaKonekcija.off('KriticanNaponUpozorenje');
        globalnaKonekcija.stop().catch(() => {});
      }
    };
  }, []);

  const ukloniAlarm = (id) => {
    setAlarmi(alarms => alarms.filter(a => a.id !== id));
  };

  if (alarmi.length === 0) return null; 

  return (
    <div style={{
      position: 'fixed', top: '20px', right: '20px', zIndex: 9999,
      display: 'flex', flexDirection: 'column', gap: '10px', maxWidth: '400px', width: '100%'
    }}>
      {alarmi.map((alarm) => (
        <div 
          key={alarm.id} 
          style={{
            backgroundColor: '#1e1e24', color: 'white', padding: '15px',
            borderRadius: '6px', borderLeft: '6px solid #ff4d4d',
            boxShadow: '0 4px 12px rgba(0,0,0,0.3)', position: 'relative'
          }}
        >
          <button 
            onClick={() => ukloniAlarm(alarm.id)}
            style={{
              position: 'absolute', top: '8px', right: '10px', background: 'none',
              border: 'none', color: '#aaa', cursor: 'pointer', fontSize: '14px', fontWeight: 'bold'
            }}
          >
            ✕
          </button>
          
          <div style={{ fontWeight: 'bold', color: '#ff4d4d', marginBottom: '5px', display: 'flex', alignItems: 'center', gap: '5px' }}>
            HITNO UPOZORENJE MREŽE
          </div>
          <p style={{ margin: '0 0 8px 0', fontSize: '14px', lineHeight: '1.4' }}>{alarm.poruka}</p>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px', color: '#aaa', borderTop: '1px solid #333', paddingTop: '6px' }}>
            <span>
              ID: <code style={{ fontFamily: 'monospace', color: '#20c997' }}>
                {alarm.brojiloId.length > 8 ? `${alarm.brojiloId.substring(0, 8)}...` : alarm.brojiloId}
              </code>
            </span>
            <span>Vreme: <strong>{alarm.vreme}</strong></span>
          </div>
        </div>
      ))}
    </div>
  );
};

export default GlobalNotificationCenter;