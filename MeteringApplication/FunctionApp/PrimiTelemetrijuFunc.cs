using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartMetering.AzureFunctions.Domain;

namespace SmartMetering.AzureFunctions
{
    public class PrimiTelemetrijuFunc
    {
        private readonly ILogger _logger;
        private readonly IBrojiloRepository _brojiloRepository;
        private readonly ITelemetrijaRepository _telemetrijaRepository;

        public PrimiTelemetrijuFunc(
            ILoggerFactory loggerFactory,
            IBrojiloRepository brojiloRepository,
            ITelemetrijaRepository telemetrijaRepository)
        {
            _logger = loggerFactory.CreateLogger<PrimiTelemetrijuFunc>();
            _brojiloRepository = brojiloRepository;
            _telemetrijaRepository = telemetrijaRepository;
        }

        [Function("PrimiTelemetriju")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetrija")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // 1. X-Device-Token header 
            if (!req.Headers.TryGetValues("X-Device-Token", out var tokenValues))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Nedostaje X-Device-Token header." }));
                return response;
            }

            string deviceToken = tokenValues.FirstOrDefault() ?? string.Empty;

            // 2. Trazenje brojila na osnovu tokena (samo uparena brojila)
            var brojilo = await _brojiloRepository.GetByDeviceTokenAsync(deviceToken);
            if (brojilo == null)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Nevažeći Device Access Token." }));
                return response;
            }

            // 3. Parsiranje tela zahteva
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<TelemetrijaZahtevDto>(requestBody);

            if (dto == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Telo zahteva nije validan JSON." }));
                return response;
            }

            // 4. Klasifikacija tarife pri prijemu (23h-07h = NT, 07h-23h = VT)
            int sat = dto.VremeMerenja.Hour;
            TipTarife tarifa = (sat >= 23 || sat < 7) ? TipTarife.NizaTarifa : TipTarife.VisaTarifa;

            // 5. Mapiranje na domensku klasu
            var telemetrija = new Telemetrija
            {
                Id = Guid.NewGuid(),
                BrojiloId = brojilo.Id,
                VremeMerenja = dto.VremeMerenja,
                UkupnaPotrosnja = dto.UkupnaPotrosnja,
                TrenutnoOpterecenje = dto.TrenutnoOpterecenje,
                Tarifa = tarifa,

                Napon = dto.Napon,
                Struja = dto.Struja,
                FaktorSnage = dto.FaktorSnage,

                NaponL1 = dto.NaponL1,
                NaponL2 = dto.NaponL2,
                NaponL3 = dto.NaponL3,
                StrujaL1 = dto.StrujaL1,
                StrujaL2 = dto.StrujaL2,
                StrujaL3 = dto.StrujaL3,
                FaktorSnageL1 = dto.FaktorSnageL1,
                FaktorSnageL2 = dto.FaktorSnageL2,
                FaktorSnageL3 = dto.FaktorSnageL3
            };

            // 6. Prioritetni upis u Telemetrije tabelu
            await _telemetrijaRepository.SaveAsync(telemetrija);

            _logger.LogInformation($"[TELEMETRIJA] Brojilo {brojilo.Id}: {telemetrija.UkupnaPotrosnja} kWh @ {telemetrija.VremeMerenja:O} ({tarifa})");

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteStringAsync(JsonConvert.SerializeObject(new
            {
                poruka = "Telemetrija uspešno primljena.",
                tarifa = tarifa.ToString()
            }));
            return response;
        }
    }


    // DTO
    public class TelemetrijaZahtevDto
    {
        public DateTime VremeMerenja { get; set; }
        public decimal UkupnaPotrosnja { get; set; }
        public decimal TrenutnoOpterecenje { get; set; }

        // Monofazni prikljucak
        public decimal? Napon { get; set; }
        public decimal? Struja { get; set; }
        public decimal? FaktorSnage { get; set; }

        // Trofazni prikljucak 
        public decimal? NaponL1 { get; set; }
        public decimal? NaponL2 { get; set; }
        public decimal? NaponL3 { get; set; }
        public decimal? StrujaL1 { get; set; }
        public decimal? StrujaL2 { get; set; }
        public decimal? StrujaL3 { get; set; }
        public decimal? FaktorSnageL1 { get; set; }
        public decimal? FaktorSnageL2 { get; set; }
        public decimal? FaktorSnageL3 { get; set; }
    }
}