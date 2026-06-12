import React from 'react';
const Unauthorized = () => <div style={{ textAlign: 'center', marginTop: '50px', color: 'red' }}><h2>403 - Nemate autorizaciju</h2><p>Vaša uloga ne dozvoljava pristup ovoj stranici.</p><a href="/login">Nazad na Login</a></div>;
export default Unauthorized;