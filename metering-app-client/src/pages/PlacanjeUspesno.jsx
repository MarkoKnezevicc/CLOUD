import React from 'react';
import { useNavigate } from 'react-router-dom';

const PlacanjeUspesno = () => {
  const navigate = useNavigate();

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', backgroundColor: '#e9ecef' }}>
      <div style={{ backgroundColor: 'white', padding: '40px', borderRadius: '8px', boxShadow: '0 4px 6px rgba(0,0,0,0.1)', textAlign: 'center', maxWidth: '400px' }}>
        <h2 style={{ color: '#28a745' }}>✅ Plaćanje uspešno!</h2>
        <p style={{ color: '#6c757d' }}>Vaš račun je uspešno plaćen. Status će biti ažuriran u Vašem nalogu.</p>
        <button onClick={() => navigate('/potrosac')} style={{ marginTop: '15px', padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
          Nazad na moje račune
        </button>
      </div>
    </div>
  );
};

export default PlacanjeUspesno;