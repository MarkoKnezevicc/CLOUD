using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using Newtonsoft.Json;
using SmartMetering.AzureFunctions.Domain;

namespace SmartMetering.AzureFunctions
{
    public class ObradaTelemetrijeQueueFunc
    {
        private readonly ILogger _logger;
        private readonly ITelemetrijaRepository _telemetrijaRepository;

        public ObradaTelemetrijeQueueFunc(ILoggerFactory loggerFactory, ITelemetrijaRepository telemetrijaRepository)
        {
            _logger = loggerFactory.CreateLogger<ObradaTelemetrijeQueueFunc>();
            _telemetrijaRepository = telemetrijaRepository;
        }

        //visestruki izlaz za upozorenje i live prikaz
        public class SignalRIzlazi
        {
            [SignalROutput(HubName = "telemetrijaHub")]
            public SignalRMessageAction SignalRMessage { get; set; }

            [SignalROutput(HubName = "telemetrijaHub")]
            public SignalRMessageAction HitnoUpozorenje { get; set; }
        }

        [Function("ObradaTelemetrijeQueue")]
        public async Task<SignalRIzlazi> Run(
            // okidac se aktivira cim se pojavi novi payload u redu cekanja
            [QueueTrigger("telemetrija-queue", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            _logger.LogInformation("[QUEUE WORKER] Počinje obrada paketa iz reda čekanja...");

            try
            {
                // 1. Odpakujemo tranzitni paket
                var paket = JsonConvert.DeserializeAnonymousType(queueItem, new { BrojiloId = "", SirovaTelemetrijaJson = "" });
                if (paket == null || string.IsNullOrEmpty(paket.SirovaTelemetrijaJson)) return null;

                // 2. Parsiramo unutrašnju telemetriju
                var dto = JsonConvert.DeserializeObject<TelemetrijaZahtevDto>(paket.SirovaTelemetrijaJson);
                if (dto == null) return null;

                // 3. Odredjivanje tarife
                int sat = dto.VremeMerenja.Hour;
                TipTarife tarifa = (sat >= 23 || sat < 7) ? TipTarife.NizaTarifa : TipTarife.VisaTarifa;

                // 4. Mapiranje na domensku klasu telemetrije
                var telemetrija = new Telemetrija
                {
                    Id = Guid.NewGuid(),
                    BrojiloId = int.Parse(paket.BrojiloId),
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

                // 5. Upis u Azure Table Storage bazu (istorijski podaci iz radnika)
                await _telemetrijaRepository.SaveAsync(telemetrija);

                SignalRMessageAction hitnoUpozorenjePoruka = null;

                // 6. Provera anomalije - Pad napona ispod 190V
                bool padNaponaDetektovan =
                    (telemetrija.Napon < 190 && telemetrija.Napon > 0) ||
                    (telemetrija.NaponL1 < 190 && telemetrija.NaponL1 > 0) ||
                    (telemetrija.NaponL2 < 190 && telemetrija.NaponL2 > 0) ||
                    (telemetrija.NaponL3 < 190 && telemetrija.NaponL3 > 0);

                if (padNaponaDetektovan)
                {
                    _logger.LogWarning($"[ALARM - QUEUE ASYNC] Kritičan napon na brojilu {paket.BrojiloId}!");

                    hitnoUpozorenjePoruka = new SignalRMessageAction("KriticanNaponUpozorenje", new object[] {
                        new {
                            poruka = "DETEKTOVAN KRITIČAN PAD NAPONA ISPOD 190V!",
                            brojiloId = paket.BrojiloId,
                            vreme = telemetrija.VremeMerenja
                        }
                    })
                    {
                        GroupName = "SvaKriticnaStanja"
                    };
                }

                // Slanje poruke kroz SignalR u grupu brojila za grafikon uzivo
                string nazivSobe = paket.BrojiloId.ToString();
                var signalrPoruka = new SignalRMessageAction("NovoMerenjeStiglo", new object[] { telemetrija })
                {
                    GroupName = nazivSobe
                };

                _logger.LogInformation($"⚙[QUEUE WORKER] Uspešno procesirana telemetrija za brojilo: {nazivSobe}");

                return new SignalRIzlazi
                {
                    SignalRMessage = signalrPoruka,
                    HitnoUpozorenje = hitnoUpozorenjePoruka
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Greška u Queue radniku: {ex.Message}");
                throw;
            }
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