using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeteringApplicationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacanjeController : ControllerBase
    {
        private readonly SmartMeteringDbContext _context;

        public PlacanjeController(SmartMeteringDbContext context)
        {
            _context = context;
        }

        [HttpPost("kreiraj-sesiju/{racunId}")]
        [Authorize(Roles = "Potrosac")]
        public async Task<IActionResult> KreirajSesiju(int racunId)
        {
            int korisnikId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var racun = await _context.Racuni.FirstOrDefaultAsync(r => r.Id == racunId && r.KorisnikId == korisnikId);
            if (racun == null) return NotFound(new { poruka = "Racun nije pronadjen." });
            if (racun.Status == StatusPlacanja.Placen) return BadRequest(new { poruka = "Racun je vec placen." });

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(racun.UkupanIznos * 100),
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Racun za struju {racun.MesecObracuna:D2}/{racun.GodinaObracuna}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = "http://localhost:5173/placanje-uspesno",
                CancelUrl = "http://localhost:5173/placanje-otkazano",
                Metadata = new Dictionary<string, string>
                {
                    { "racunId", racun.Id.ToString() }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            racun.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();

            return Ok(new { url = session.Url });
        }
    }
}