import React, {useState, useEffect} from 'react';
import {authService} from '../services/authService';

const NaplataDashboard = () => {

  const [tarife, setTarife] = useState([]);
  const [greska, setGreska] = useState('');
  const [poruka, setPoruka] = useState('');

  const [novaTarifa, setNovaTarifa] = useState({
    cenaZ_VT: '', cenaZ_NT: '',
    cenaP_VT: '', cenaP_NT: '',
    cenaC_VT: '', cenaC_NT: '',
    cenaObracunskeSnage: '', trosakSnabdevaca: ''
  });

  const getHeaders = () => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authService.getToken()}`
  });

  const ucitajTarife = async () => {
    try {
      const res = await fetch ('https://localhost:7078/api/obracun/tarife', {headers: getHeaders() });
      if (res.ok) setTarife(await res.json());
    } catch {
      setGreska('Greska pri ucitavanju tarifa.');
    }
  };

  useEffect(() => {ucitajTarife(); }, []);

  const handleKreirajTarifu = async (e) => {
    e.preventDefault();
    setGreska(''); setPoruka('');
    try {
      const res = await fetch ('https://localhost:7078/api/obracun/tarife', {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({
          CenaZ_VT: parseFloat(novaTarifa.cenaZ_VT),
          CenaZ_NT: parseFloat(novaTarifa.cenaZ_NT),
          CenaP_VT: parseFloat(novaTarifa.cenaP_VT),
          CenaP_NT: parseFloat(novaTarifa.cenaP_NT),
          CenaC_VT: parseFloat(novaTarifa.cenaC_VT),
          CenaC_NT: parseFloat(novaTarifa.cenaC_NT),
          CenaObracunskeSnage: parseFloat(novaTarifa.cenaObracunskeSnage),
          TrosakSnabdevaca: parseFloat(novaTarifa.trosakSnabdevaca)
        })
      });
      if (res.ok) {
        setPoruka('Tarifni model kreiran i aktivan!');
        setNovaTarifa({ cenaZ_VT: '', cenaZ_NT: '', cenaP_VT: '', cenaP_NT: '', cenaC_VT: '', cenaC_NT: '', cenaObracunskeSnage: '', trosakSnabdevaca: ''});
        ucitajTarife();
      } else {
        const err = await res.json();
        setGreska(err.poruka || 'Greska.');
      }
    } catch (err) {
      console.error('GREŠKA:', err);
      setGreska('Sistemska greška: ' + err.message);
    }
  };

  return (
    <div style={{ fontFamily: 'Arial, sans-serif', padding: '20px'}}>
      <h3 style = {{ color: '#2c3e50', borderBottom: '2px solid #34495e', paddingBottom: '10px'}}>
        Upravljanje tarifnim modelom
      </h3>

      {greska && <div style = {{ color: 'red', backgroundColor: '#f8d7da', padding: '10px', borderRadius: '4px', marginBottom: '15px' }}>{greska}</div>}
      {poruka && <div style = {{ color: 'green', backgroundColor: '#d4edda', padding: '10px', borderRadius: '4px', marginBottom: '15px '}}>{poruka}</div>}

      {/* Forma za novu tarifu */}
      <div style = {{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', marginBottom: '30px'}}>
        <h4>Kreiraj novi tarifni model</h4>
        <form onSubmit={handleKreirajTarifu}>

          <p style = {{ fontWeight: 'bold', marginBottom: '5px'}}>Zelena zona (0-350kwh)</p>
          <div style = {{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '15px'}}>
            <input type="number" step="0.0001" placeholder="VT cena (RSD/kwh)" value = {novaTarifa.cenaZ_VT} onChange={e => setNovaTarifa({...novaTarifa, cenaZ_VT: e.target.value})} required style = {{ padding: '8px'}} />
            <input type="number" step="0.0001" placeholder="NT cena (RSD/kwh)" value = {novaTarifa.cenaZ_NT} onChange={e => setNovaTarifa({...novaTarifa, cenaZ_NT: e.target.value})} required style = {{ padding: '8px'}} />
          </div> 

          <p style = {{ fontWeight: 'bold', marginBottom: '5px'}}>Plava zona (351-1200 kwh)</p>
          <div style = {{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '15px'}}>
            <input type="number" step = "0.0001" placeholder = "VT cena (RSD/kwh)" value = {novaTarifa.cenaP_VT} onChange = {e => setNovaTarifa({...novaTarifa, cenaP_VT: e.target.value})} required style = {{ padding: '8px'}}/>
            <input type="number" step = "0.0001" placeholder = "NT cena (RSD/kwh)" value = {novaTarifa.cenaP_NT} onChange = {e => setNovaTarifa({...novaTarifa, cenaP_NT: e.target.value})} required style = {{ padding: '8px'}}/>
          </div>
          
          <p style = {{ fontWeight: 'bold', marginBottom: '5px'}}>Crvena zona(&gt;1200 kwh)</p>
          <div style = {{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '15px'}}>
            <input type="number" step="0.0001" placeholder = "VT cena (RSD/kwh)" value = {novaTarifa.cenaC_VT} onChange = {e => setNovaTarifa({...novaTarifa, cenaC_VT: e.target.value})} required style = {{ padding: '8px'}}/>
            <input type="number" step="0.0001" placeholder = "NT cena (RSD/kwh)" value = {novaTarifa.cenaC_NT} onChange = {e => setNovaTarifa({...novaTarifa, cenaC_NT: e.target.value})} required style = {{ padding: '8px'}}/>
          </div>

          <p style = {{ fontWeight: 'bold', marginBottom: '5px'}}>Fiksni Troskovi</p>
          <div style = {{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '15px'}}>
            <input type="number" step="0.0001" placeholder="Cena obracunske snage (RSD/kw)" value = {novaTarifa.cenaObracunskeSnage} onChange = {e => setNovaTarifa({...novaTarifa, cenaObracunskeSnage: e.target.value})} required style = {{ padding: '8px'}}/>
            <input type="number" step="0.0001" placeholder="Trosak snabdevaca (RSD)" value = {novaTarifa.trosakSnabdevaca} onChange={e => setNovaTarifa({...novaTarifa, trosakSnabdevaca: e.target.value})} required style = {{ padding: '8px'}}/>
          </div>

          <button type="submit" style = {{ backgroundColor: '#28a745', color: 'white', border: 'none', padding: '10px 20px', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold'}}>
            Aktiviraj tarifni model
          </button>
        </form>
      </div>

      {/* Prikaz istorije tarifa */}
      <div style = {{ backgroundColor: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)' }}>
        <h4>Istorija tarifnih modela</h4>
        {tarife.length === 0 ? (
          <p style = {{ color: '#6c757d'}}>Nema unetih tarifnih modela.</p>
        ) : (
          <table style = {{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style = {{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6', textAlign: 'left' }}>
                <th style = {{ padding: '10px' }}>Datum</th>
                <th>Z-VT</th><th>Z-NT</th>
                <th>P-VT</th><th>P-NT</th>
                <th>C-VT</th><th>C-NT</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {tarife.map(t => (
                <tr key={t.id} style = {{ borderBottom: '1px solid #dee2e6' }}>
                  <td style = {{ padding: '10px'}}>{new Date(t.datumKreiranja).toLocaleDateString('sr-RS')}</td>
                  <td>{t.cenaZ_VT}</td><td>{t.cenaZ_NT}</td>
                  <td>{t.cenaP_VT}</td><td>{t.cenaP_NT}</td>
                  <td>{t.cenaC_VT}</td><td>{t.cenaC_NT}</td>
                  <td>
                    <span style = {{ backgroundColor: t.isAktivan ? '#d4edda' : '#f8d7da', color : t.isAktivan ? '#155724' : '#721c24', padding: '3px 8px', borderRadius: '4px', fontSize: '13px', fontWeight: 'bold'}}>
                    {t.isAktivan ? 'Aktivan' : 'Neaktivan'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default NaplataDashboard;