import { jwtDecode } from 'jwt-decode';

export const authService = {
  saveToken: (token) => {
    localStorage.setItem('token', token);
  },

  getToken: () => {
    return localStorage.getItem('token');
  },

  clearToken: () => {
    localStorage.removeItem('token');
  },

  getUser: () => {
    const token = localStorage.getItem('token');
    if (!token) return null;

    try {
      const decoded = jwtDecode(token);
      console.log("Dekodirani token unutar servisa:", decoded); // Ovo će nam ispisati tačna polja

      // Hvata ulogu bilo da je role, Role, uloga, Uloga ili MS Claim
      const uloga = 
                    decoded.role || 
                    decoded.Role || 
                    decoded.uloga || 
                    decoded.Uloga;
                    
      // Hvata email na isti način
      const email = 
                    decoded.email || 
                    decoded.Email;

      const id = 
                 decoded.id || 
                 decoded.Id;

      // VRAĆAMO ČIST OBJEKAT GDE SU SVA POLJA MALIM SLOVIMA
      return {
        id: id,
        email: email,
        uloga: uloga // Garantovano malim slovima ključ "uloga"
      };
    } catch (error) {
      console.error("Greška pri dekodiranju JWT tokena:", error);
      return null;
    }
  }
};