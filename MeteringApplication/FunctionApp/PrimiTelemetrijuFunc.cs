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

        // Klasa koja omogućava istovremeno vraćanje HTTP odgovora simulatoru i slanje kroz SignalR
        public class VisestrukiIzlaz
        {
            public HttpResponseData HttpResponse { get; set; }


            //Propety za slanje poruka odredjenoj sobi
            [SignalROutput(HubName = "telemetrijaHub")]
            public SignalRMessageAction SignalRMessage { get; set; }
        }

        [Function("PrimiTelemetriju")]
        public async Task<VisestrukiIzlaz> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetrija")] HttpRequestData req)
        {
            var httpResponse = req.CreateResponse();
            httpResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // 1. Provera X-Device-Token-a iz zaglavlja zahteva
            if (!req.Headers.TryGetValues("X-Device-Token", out var tokenValues))
            {
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Nedostaje X-Device-Token header." }));
                return new VisestrukiIzlaz { HttpResponse = httpResponse, SignalRMessage = null };
            }

            string deviceToken = tokenValues.FirstOrDefault() ?? string.Empty;

            // 2. Pronalaženje brojila u SQL bazi na osnovu poslatog tokena
            var brojilo = await _brojiloRepository.GetByDeviceTokenAsync(deviceToken);
            if (brojilo == null)
            {
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Nevažeći Device Access Token." }));
                return new VisestrukiIzlaz { HttpResponse = httpResponse, SignalRMessage = null };
            }

            // 3. Čitanje i parsiranje JSON tela koje šalje simulator
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<TelemetrijaZahtevDto>(requestBody);

            if (dto == null)
            {
                httpResponse.StatusCode = HttpStatusCode.BadRequest;
                await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Telo zahteva nije validan JSON." }));
                return new VisestrukiIzlaz { HttpResponse = httpResponse, SignalRMessage = null };
            }

            // 4. Određivanje tarife (od 23h do 07h je Niža/Jeftina tarifa)
            int sat = dto.VremeMerenja.Hour;
            TipTarife tarifa = (sat >= 23 || sat < 7) ? TipTarife.NizaTarifa : TipTarife.VisaTarifa;

            // 5. Mapiranje na domensku klasu telemetrije
            var telemetrija = new Telemetrija
            {
                Id = Guid.NewGuid(),
                BrojiloId = brojilo.Id, // GUID brojila iz baze podataka
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

            //Upis u Azure Table Storage bazu (istorijski podaci)
            await _telemetrijaRepository.SaveAsync(telemetrija);

            //Soba je imenovana po id-u brojila
            string nazivSobe = brojilo.Id.ToString();
            _logger.LogInformation($"[SIGNALR REALTIME] Šaljem podatke uživo u sobu brojila: {nazivSobe}");

            //Slanje poruke kroz Azure SignalR servis u izabranu sobu
            var signalrPoruka = new SignalRMessageAction("NovoMerenjeStiglo", new object[] { telemetrija })
            {
                GroupName = nazivSobe
            };

            // Formiranje uspešnog HTTP odgovora za simulator
            httpResponse.StatusCode = HttpStatusCode.OK;
            await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new
            {
                poruka = "Telemetrija uspešno primljena.",
                tarifa = tarifa.ToString()
            }));

            return new VisestrukiIzlaz
            {
                HttpResponse = httpResponse,
                //saljemo poruku sobi
                SignalRMessage = signalrPoruka
            };
        }
    }

    public class TelemetrijaZahtevDto
    {
        public DateTime VremeMerenja { get; set; }
        public decimal UkupnaPotrosnja { get; set; }
        public decimal TrenutnoOpterecenje { get; set; }

        public decimal? Napon { get; set; }
        public decimal? Struja { get; set; }
        public decimal? FaktorSnage { get; set; }

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