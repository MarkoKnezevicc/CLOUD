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