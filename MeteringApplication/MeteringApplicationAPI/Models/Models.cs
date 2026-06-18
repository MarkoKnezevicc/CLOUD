using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public enum UlogaKorisnika
{
    SistemskiAdmin,
    Potrosac,
    AdministratorNaplate
}

public enum TipPrikljucka
{
    Monofazni,
    Trofazni
}

public enum StatusUredjaja
{
    Neuparen,
    Uparen
}

public class Korisnik
{
    public int Id { get; set; }

    [Required]
    public string Ime { get; set; }

    [Required]
    public string Prezime { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string Telefon { get; set; }

    [Required]
    public string LozinkaHash { get; set; }

    public UlogaKorisnika Uloga { get; set; }

    public bool IsActive { get; set; }

    public ICollection<Objekat> Objekti { get; set; } = new List<Objekat>();
}

public class Objekat
{
    public int Id { get; set; }

    [Required]
    public string Naziv { get; set; }

    [Required]
    public string Grad { get; set; }

    [Required]
    public string Adresa { get; set; }

    public string Opis { get; set; }

    public int KorisnikId { get; set; }
    public Korisnik Korisnik { get; set; }

    public ICollection<PametnoBrojilo> PametnaBrojila { get; set; } = new List<PametnoBrojilo>();
}

public class PametnoBrojilo
{
    public int Id { get; set; }

    [Required]
    public string SerijskiBroj { get; set; }

    public TipPrikljucka Tip { get; set; }

    public decimal MaksimalnaOdobrenaSnaga { get; set; }

    public string Napomena { get; set; }

    public StatusUredjaja Status { get; set; }

    
    public string? Uuid { get; set; }

    public string? DeviceAccessToken { get; set; }

    public int ObjekatId { get; set; }
    public Objekat Objekat { get; set; }
}

public class TarifniModel
{
    public int Id { get; set; }

    // Zelena zona (0-350 kwh)
    public decimal CenaZ_VT { get; set; }
    public decimal CenaZ_NT { get; set; }

    // Plava zona (351-1200 kwh)
    public decimal CenaP_VT { get; set; }
    public decimal CenaP_NT { get; set; }

    // Crvena zona (>1200 kwh)
    public decimal CenaC_VT { get; set; }
    public decimal CenaC_NT { get; set; }

    // Fiksni troskovi
    public decimal CenaObracunskeSnage { get; set; }
    public decimal TrosakSnabdevaca { get; set; }
    public DateTime DatumKreiranja { get; set; }
    public bool IsAktivan { get; set; }
}

public enum StatusPlacanja
{
    Neplacen,
    Placen
}

public class Racun
{
    public int Id { get; set; }
    public int BrojiloId { get; set; }
    public PametnoBrojilo Brojilo { get; set; }
    public int KorisnikId { get; set; }
    public Korisnik Korisnik { get; set; }
    public int GodinaObracuna { get; set; }
    public int MesecObracuna { get; set; }

    // Ukupna potrosnja po tarifi (kwh)
    public decimal EnergijaVT { get; set; }
    public decimal EnergijaNT { get; set; }

    // Iznosi po zonama
    public decimal IznosZelena { get; set; }
    public decimal IznosPlava { get; set; }
    public decimal IznosCrvena { get; set; }
    public decimal FiksniTroskovi { get; set; }
    public decimal UkupanIznos {  get; set; }
    public StatusPlacanja Status { get; set; }
    public DateTime DatumIzdavanja { get; set; }

    // Tekstualni format racuna - ceo tekst racuna se cuva
    public string TekstRacuna { get; set; }
}