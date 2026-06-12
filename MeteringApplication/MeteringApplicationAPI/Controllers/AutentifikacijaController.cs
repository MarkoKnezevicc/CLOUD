using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Lozinka { get; set; } = string.Empty;
}

public class RegistracijaDto
{
    public string Ime { get; set; } = string.Empty;
    public string Prezime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Lozinka { get; set; } = string.Empty;
    public UlogaKorisnika Uloga { get; set; }
}


[ApiController]
[Route("api/[controller]")]
public class AutentifikacijaController : ControllerBase
{
    private readonly SmartMeteringDbContext _context;
    private readonly TokenService _tokenService;

    public AutentifikacijaController(SmartMeteringDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {

        var korisnik = await _context.Korisnici
            .FirstOrDefaultAsync(k => k.Email == model.Email);


        if (korisnik == null || !BCrypt.Net.BCrypt.Verify(model.Lozinka, korisnik.LozinkaHash))
        {
            return Unauthorized(new { poruka = "Neispravan email ili lozinka!" });
        }


        var token = _tokenService.GenerisiToken(korisnik);

        return Ok(new
        {
            Token = token,
            Ime = korisnik.Ime,
            Prezime = korisnik.Prezime,
            Uloga = korisnik.Uloga.ToString()
        });
    }

    [HttpPost("kreiraj-korisnika")]
    public async Task<IActionResult> KreirajKorisnika([FromBody] RegistracijaDto model)
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

        return Ok(new { poruka = "Korisnik uspešno kreiran!" });
    }
}


