using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MeteringApplicationAPI.Controllers
{
    public class TarifniModelKreiranjeDto
    {
        public decimal CenaZ_VT { get; set; }
        public decimal CenaZ_NT { get; set; }
        public decimal CenaP_VT { get; set; }
        public decimal CenaP_NT { get; set; }
        public decimal CenaC_VT { get; set; }
        public decimal CenaC_NT { get; set; }
        public decimal CenaObracunskeSnage { get; set; }
        public decimal TrosakSnabdevaca { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ObracunController : ControllerBase
    {
        private readonly SmartMeteringDbContext _context;

        public ObracunController(SmartMeteringDbContext context)
        {
            _context = context;
        }

        // Administrator naplate
        // Tarifni modeli

        [HttpGet("tarife")]
        [Authorize(Roles = "AdministratorNaplate")]
        public async Task<IActionResult> VratiSveTarife()
        {
            var tarife = await _context.TarifniModeli
                .OrderByDescending(t =>
                t.DatumKreiranja)
                .ToListAsync();
            return Ok(tarife);
        }

        [HttpPost("tarife")]
        [Authorize(Roles = "AdministratorNaplate")]
        public async Task<IActionResult> KreirajTarifu([FromBody] TarifniModelKreiranjeDto dto)
        {
            // Deaktivacija postojecih
            var postojece = await _context.TarifniModeli.Where(t =>
                t.IsAktivan).ToListAsync();
            foreach (var t in postojece)
                t.IsAktivan = false;

            var novaTarifa = new TarifniModel
            {
                CenaZ_VT = dto.CenaZ_VT,
                CenaZ_NT = dto.CenaZ_NT,
                CenaP_VT = dto.CenaP_VT,
                CenaP_NT = dto.CenaP_NT,
                CenaC_VT = dto.CenaC_VT,
                CenaC_NT = dto.CenaC_NT,
                CenaObracunskeSnage = dto.CenaObracunskeSnage,
                TrosakSnabdevaca = dto.TrosakSnabdevaca,
                DatumKreiranja = DateTime.UtcNow,
                IsAktivan = true
            };

            _context.TarifniModeli.Add(novaTarifa);
            await _context.SaveChangesAsync();

            return Ok(new { poruka = "Tarifni model kreiran i aktivan!" });
        }

        // Racuni
        [HttpGet("racuni/{brojiloId}")]
        [Authorize(Roles = "Potrosac")]
        public async Task<IActionResult> VratiRacuneBrojila(int brojiloId)
        {
            int korisnikId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var racuni = await _context.Racuni
            .Where(r => r.BrojiloId == brojiloId && r.KorisnikId == korisnikId)
            .OrderByDescending(r => r.GodinaObracuna)
            .ThenByDescending(r => r.MesecObracuna)
            .Select(r => new
            {
                r.Id,
                r.GodinaObracuna,
                r.MesecObracuna,
                r.EnergijaVT,
                r.EnergijaNT,
                r.IznosZelena,
                r.IznosPlava,
                r.IznosCrvena,
                r.FiksniTroskovi,
                r.UkupanIznos,
                Status = r.Status.ToString(),
                r.DatumIzdavanja,
                r.TekstRacuna
            })
            .ToListAsync();

            return Ok(racuni);
        }
    }
}
