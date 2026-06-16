using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


public class KorisnikPrikazDto
{
    public int Id { get; set; }
    public string Ime { get; set; } = string.Empty;
    public string Prezime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Uloga { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class KorisnikKreiranjeDto
{
    [Required]
    public string Ime { get; set; } = string.Empty;

    [Required]
    public string Prezime { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;

    [Required]
    public string Lozinka { get; set; } = string.Empty;

    [Required]
    public UlogaKorisnika Uloga { get; set; }
}

public class PromenaStatusaDto
{
    public bool IsActive { get; set; }
}
public class BrojiloPrikazDto
{
    public int Id { get; set; }
    public string NazivObjekta { get; set; } = string.Empty;
    public string AdresaObjekta { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SistemskiAdmin")] 

public class AdminController : ControllerBase
{
    private readonly SmartMeteringDbContext _context;

    public AdminController(SmartMeteringDbContext context)
    {
        _context = context;
    }

    
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var korisnici = await _context.Korisnici
            .Select(k => new KorisnikPrikazDto
            {
                Id = k.Id,
                Ime = k.Ime,
                Prezime = k.Prezime,
                Email = k.Email,
                Telefon = k.Telefon,
                Uloga = k.Uloga.ToString(),
                IsActive = k.IsActive
            })
            .ToListAsync();

        return Ok(korisnici);
    }

    [HttpGet("users/{userId}/meters")]
    public async Task<IActionResult> GetUserMeters(int userId)
    {
        var korisnikPostoji = await _context.Korisnici.AnyAsync(k => k.Id == userId);
        if (!korisnikPostoji)
        {
            return NotFound(new { poruka = "Korisnik nije pronađen!" });
        }

        // Izvlačimo sva brojila sa svih objekata koji pripadaju ovom korisniku
        var brojila = await _context.PametnaBrojila
            .Where(b => b.Objekat.KorisnikId == userId)
            .Select(b => new BrojiloPrikazDto
            {
                Id = b.Id,
                NazivObjekta = b.Objekat.Naziv,
                AdresaObjekta = b.Objekat.Grad + ", " + b.Objekat.Adresa,
                Tip = b.Tip.ToString(),
                Status = b.Status.ToString()
            })
            .ToListAsync();

        return Ok(brojila);
    }


    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] KorisnikKreiranjeDto model)
    {
        if (await _context.Korisnici.AnyAsync(k => k.Email == model.Email))
        {
            return BadRequest(new { poruka = "Korisnik sa ovim email-om već postoji!" });
        }

        var noviKorisnik = new Korisnik
        {
            Ime = model.Ime,
            Prezime = model.Prezime,
            Email = model.Email,
            Telefon = model.Telefon,
            LozinkaHash = BCrypt.Net.BCrypt.HashPassword(model.Lozinka), 
            Uloga = model.Uloga,
            IsActive = true
        };

        _context.Korisnici.Add(noviKorisnik);
        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Korisnik uspešno kreiran od strane admina!" });
    }

    
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] PromenaStatusaDto model)
    {
        
        var trenutniAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (id.ToString() == trenutniAdminId)
        {
            return BadRequest(new { poruka = "Ne možete suspendovati sopstveni nalog!" });
        }

        var korisnik = await _context.Korisnici.FindAsync(id);
        if (korisnik == null)
        {
            return NotFound(new { poruka = "Korisnik nije pronađen!" });
        }

        korisnik.IsActive = model.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Status uspešno promenjen!" });
    }

    
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        
        var trenutniAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (id.ToString() == trenutniAdminId)
        {
            return BadRequest(new { poruka = "Ne možete obrisati sopstveni nalog!" });
        }

        var korisnik = await _context.Korisnici.FindAsync(id);
        if (korisnik == null)
        {
            return NotFound(new { poruka = "Korisnik nije pronađen!" });
        }

        _context.Korisnici.Remove(korisnik);
        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Korisnik obrisan!" });
    }
}