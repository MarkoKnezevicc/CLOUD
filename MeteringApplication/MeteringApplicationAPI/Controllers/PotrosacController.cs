using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class ObjekatKreiranjeDto
{
    public string? Naziv { get; set; }
    public string? Grad { get; set; }
    public string? Adresa { get; set; }
    public string? Opis { get; set; }
}

public class BrojiloRegistracijaDto
{
    public string? SerijskiBroj { get; set; }
    public string? Tip { get; set; }
    public string? Napomena { get; set; }
}
public class LimitPodesavanjeDto
{
    public decimal? LimitVrednost { get; set; }
    public string? LimitJedinica { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Potrosac")]
public class PotrosacController : ControllerBase
{
    private readonly SmartMeteringDbContext _context;

    public PotrosacController(SmartMeteringDbContext context)
    {
        _context = context;
    }

    private int VratiTrenutnogKorisnikaId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim ?? "0");
    }

    
    [HttpGet("objekti")]
    public async Task<IActionResult> VratiMojeObjekte()
    {
        int korisnikId = VratiTrenutnogKorisnikaId();

        var objekti = await _context.Objekti
            .Where(o => o.KorisnikId == korisnikId)
            .Include(o => o.PametnaBrojila)
            .Select(o => new
            {
                o.Id,
                o.Naziv,
                o.Grad,
                o.Adresa,
                o.Opis,
                Brojila = o.PametnaBrojila.Select(b => new
                {
                    b.Id,
                    Tip = b.Tip.ToString(),
                    b.MaksimalnaOdobrenaSnaga,
                    b.SerijskiBroj,
                    Status = b.Status.ToString(),
                    b.Napomena,
                    b.LimitVrednost,
                    LimitJedinica = b.LimitJedinica.HasValue ? b.LimitJedinica.ToString() : null
                })
            }).ToListAsync();

        return Ok(objekti);
    }

    
    [HttpPost("objekti")]
    public async Task<IActionResult> DodajObjekat([FromBody] ObjekatKreiranjeDto dto)
    {
        int korisnikId = VratiTrenutnogKorisnikaId();

        var noviObjekat = new Objekat
        {
            Naziv = dto.Naziv ?? "",
            Grad = dto.Grad ?? "",
            Adresa = dto.Adresa ?? "",
            Opis = dto.Opis ?? "",
            KorisnikId = korisnikId
        };

        _context.Objekti.Add(noviObjekat);
        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Objekat uspešno kreiran!", objekatId = noviObjekat.Id });
    }

    [HttpGet("objekti/{objekatId}/brojila")]
    public async Task<IActionResult> VratiBrojila(int objekatId)
    {
        int korisnikId = VratiTrenutnogKorisnikaId();

        var objekat = await _context.Objekti
            .FirstOrDefaultAsync(o => o.Id == objekatId &&
                                      o.KorisnikId == korisnikId);

        if (objekat == null)
            return NotFound(new { poruka = "Objekat nije pronađen." });

        var brojila = await _context.PametnaBrojila
            .Where(b => b.ObjekatId == objekatId)
            .Select(b => new
            {
                b.Id,
                b.SerijskiBroj,
                Tip = b.Tip.ToString(),
                b.MaksimalnaOdobrenaSnaga,
                Status = b.Status.ToString(),
                b.Napomena
            })
            .ToListAsync();

        return Ok(brojila);
    }

    
    [HttpPost("objekti/{objekatId}/brojila")]
    public async Task<IActionResult> RegistrujBrojilo([FromRoute] int objekatId, [FromBody] BrojiloRegistracijaDto dto)
    {
        int korisnikId = VratiTrenutnogKorisnikaId();

        
        var objekat = await _context.Objekti.FirstOrDefaultAsync(o => o.Id == objekatId && o.KorisnikId == korisnikId);
        if (objekat == null)
        {
            return NotFound(new { poruka = "Objekat nije pronađen ili nemate pravo pristupa." });
        }

        if (string.IsNullOrEmpty(dto.SerijskiBroj))
        {
            return BadRequest(new { poruka = "Serijski broj je obavezan." });
        }


        var regex = new Regex(@"^S[AM]-\d{4}-\d{5}$");
        if (!regex.IsMatch(dto.SerijskiBroj))
        {
            return BadRequest(new { poruka = "Serijski broj mora biti u formatu SA-YYYY-XXXXX" });
        }

        var postojiBrojilo = await _context.PametnaBrojila.AnyAsync(b => b.SerijskiBroj == dto.SerijskiBroj);
        if (postojiBrojilo)
        {
            return BadRequest(new { poruka = $"Brojilo sa serijskim brojem '{dto.SerijskiBroj}' je već registrovano u sistemu!" });
        }

        
        if (string.IsNullOrEmpty(dto.Tip) || !Enum.TryParse<TipPrikljucka>(dto.Tip, true, out var tipPrikljucka))
        {
            return BadRequest(new { poruka = "Neispravan tip priključka (Monofazni/Trofazni)." });
        }

        
        decimal snaga = tipPrikljucka == TipPrikljucka.Trofazni ? 11.04m : 6.9m;

        var novoBrojilo = new PametnoBrojilo
        {
            SerijskiBroj = dto.SerijskiBroj,
            Tip = tipPrikljucka,
            MaksimalnaOdobrenaSnaga = snaga,
            Napomena = dto.Napomena ?? "",
            Status = StatusUredjaja.Neuparen,
            Uuid = null,               
            DeviceAccessToken = null,  
            ObjekatId = objekatId
        };

        _context.PametnaBrojila.Add(novoBrojilo);
        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Brojilo uspešno registrovano sa statusom Neuparen!" });
    }

    [HttpPut("brojila/{brojiloId}/limit")]
    public async Task<IActionResult> PodesiLimit(int brojiloId, [FromBody] LimitPodesavanjeDto dto)
    {
        int korisnikId = VratiTrenutnogKorisnikaId();

        var brojilo = await _context.PametnaBrojila
            .Include(b => b.Objekat)
            .FirstOrDefaultAsync(b => b.Id == brojiloId && b.Objekat.KorisnikId == korisnikId);

        if (brojilo == null)
            return NotFound(new { poruka = "Brojilo nije pronađeno ili nemate pravo pristupa." });

        if (dto.LimitVrednost == null)
        {
            brojilo.LimitVrednost = null;
            brojilo.LimitJedinica = null;
        }
        else
        {
            if (dto.LimitVrednost.Value <= 0)
                return BadRequest(new { poruka = "Limit mora biti veći od nule." });

            if (string.IsNullOrEmpty(dto.LimitJedinica) || !Enum.TryParse<JedinicaLimita>(dto.LimitJedinica, true, out var jedinica))
                return BadRequest(new { poruka = "Neispravna jedinica limita (KWh/RSD)." });

            brojilo.LimitVrednost = dto.LimitVrednost;
            brojilo.LimitJedinica = jedinica;
        }

        brojilo.UpozorenjePoslato = false;

        await _context.SaveChangesAsync();

        return Ok(new { poruka = "Limit potrošnje uspešno ažuriran." });
    }
}

