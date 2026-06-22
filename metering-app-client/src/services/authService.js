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
      console.log("Dekodirani token unutar servisa:", decoded);

      if (decoded.exp && decoded.exp * 1000 < Date.now()) {
        localStorage.removeItem('token');
        return null;
      }


      const stvarnaUloga = decoded.role || decoded.Role || 'Potrosac';
                    
      
      const email = decoded.email || decoded.Email;

      
      const id = decoded.nameid || decoded.id || decoded.Id;

      
      return {
        id: id,
        email: email,
        uloga: stvarnaUloga, 
        rola: stvarnaUloga, 
        role: stvarnaUloga   
      };
    } catch (error) {
      console.error("Greška pri dekodiranju JWT tokena:", error);
      localStorage.removeItem('token');
      return null;
    }
  }
};