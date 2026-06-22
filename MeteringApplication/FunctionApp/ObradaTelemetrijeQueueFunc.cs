using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using Newtonsoft.Json;
using SmartMetering.AzureFunctions.Domain;
using Microsoft.Data.SqlClient;
using SendGrid;
using SendGrid.Helpers.Mail;

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

            [SignalROutput(HubName = "telemetrijaHub")]
            public SignalRMessageAction MrezniHeartbeat { get; set; }
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

                // Limit potrosnje
                await ProveriLimitPotrosnje(telemetrija, paket.BrojiloId);

                var mrezniHeartbeatPoruka = new SignalRMessageAction("MrezniHeartbeatStigao", new object[] {
                new {
                    brojiloId = telemetrija.BrojiloId, // Saljemo int ID brojila
                    vreme = telemetrija.VremeMerenja,
                    ukupnaPotrosnja = telemetrija.UkupnaPotrosnja
                }
                    })
                {
                    GroupName = "SveMrezneAktivnosti" // Svi admini na tabelama slušaju ovu grupu
                };

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
                    HitnoUpozorenje = hitnoUpozorenjePoruka,
                    MrezniHeartbeat = mrezniHeartbeatPoruka
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Greška u Queue radniku: {ex.Message}");
                throw;
            }
        }

        private async Task ProveriLimitPotrosnje(Telemetrija telemetrija, string brojiloIdStr)
        {
            string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            using SqlConnection conn = new SqlConnection(sqlConnectionString);
            await conn.OpenAsync();

            // KORAK 1: Ucitavanje podataka o limitu i korisniku
            string selectQuery = @"SELECT pb.LimitVrednost, pb.LimitJedinica, pb.PocetnoStanjeMeseca,
                                   pb.PracenjeMesec, pb.PracenjeGodina, pb.UpozorenjePoslato, k.Email
                            FROM PametnaBrojila pb
                            INNER JOIN Objekti o ON o.Id = pb.ObjekatId
                            INNER JOIN Korisnici k ON k.Id = o.KorisnikId
                            WHERE pb.Id = @Id";

            decimal? limitVrednost = null;
            int? limitJedinica = null;
            decimal pocetnoStanje = 0;
            int pracenjeMesec = 0;
            int pracenjeGodina = 0;
            bool upozorenjePoslato = false;
            string email = "";

            using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", int.Parse(brojiloIdStr));
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;

                limitVrednost = reader.IsDBNull(0) ? (decimal?)null : reader.GetDecimal(0);
                limitJedinica = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                pocetnoStanje = reader.GetDecimal(2);
                pracenjeMesec = reader.GetInt32(3);
                pracenjeGodina = reader.GetInt32(4);
                upozorenjePoslato = reader.GetBoolean(5);
                email = reader.GetString(6);
            }

            if (limitVrednost == null) return; // Korisnik nije podesio limit

            int trenutniMesec = telemetrija.VremeMerenja.Month;
            int trenutnaGodina = telemetrija.VremeMerenja.Year;

            // KORAK 2: Provera da li je novi mesec u odnosu na ono sto pamtimo
            if (trenutniMesec != pracenjeMesec || trenutnaGodina != pracenjeGodina)
            {
                string resetQuery = @"UPDATE PametnaBrojila 
                               SET PocetnoStanjeMeseca = @Pocetno, PracenjeMesec = @Mesec, 
                                   PracenjeGodina = @Godina, UpozorenjePoslato = 0
                               WHERE Id = @Id";
                using SqlCommand resetCmd = new SqlCommand(resetQuery, conn);
                resetCmd.Parameters.AddWithValue("@Pocetno", telemetrija.UkupnaPotrosnja);
                resetCmd.Parameters.AddWithValue("@Mesec", trenutniMesec);
                resetCmd.Parameters.AddWithValue("@Godina", trenutnaGodina);
                resetCmd.Parameters.AddWithValue("@Id", int.Parse(brojiloIdStr));
                await resetCmd.ExecuteNonQueryAsync();

                _logger.LogInformation($"[LIMIT] Brojilo {brojiloIdStr}: novi mesec, pocetno stanje resetovano.");
                return; // Tek smo resetovali, potrosnja meseca je 0, nema sta da provericamo
            }

            if (upozorenjePoslato) return; // Vec smo poslali upozorenje ovog meseca

            decimal potrosnjaMeseca = telemetrija.UkupnaPotrosnja - pocetnoStanje;
            if (potrosnjaMeseca < 0) return;

            decimal trenutnaVrednostZaPoredjenje;

            // JedinicaLimita: KWh = 0, RSD = 1 (po redosledu u enumu)
            if (limitJedinica == 0)
            {
                trenutnaVrednostZaPoredjenje = potrosnjaMeseca;
            }
            else
            {
                decimal prosecnaCena = await UcitajProsecnuCenuPoKwh(conn);
                if (prosecnaCena == 0) return; // Nema aktivnog tarifnog modela
                trenutnaVrednostZaPoredjenje = potrosnjaMeseca * prosecnaCena;
            }

            if (trenutnaVrednostZaPoredjenje >= limitVrednost.Value)
            {
                _logger.LogWarning($"[LIMIT] Brojilo {brojiloIdStr}: PREKORACEN limit potrosnje! ({trenutnaVrednostZaPoredjenje:F2} >= {limitVrednost.Value:F2})");

                await PosaljiMejlUpozorenje(email, brojiloIdStr, potrosnjaMeseca, trenutnaVrednostZaPoredjenje, limitVrednost.Value, limitJedinica == 1);

                string azurirajQuery = "UPDATE PametnaBrojila SET UpozorenjePoslato = 1 WHERE Id = @Id";
                using SqlCommand azurirajCmd = new SqlCommand(azurirajQuery, conn);
                azurirajCmd.Parameters.AddWithValue("@Id", int.Parse(brojiloIdStr));
                await azurirajCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<decimal> UcitajProsecnuCenuPoKwh(SqlConnection conn)
        {
            string query = @"SELECT TOP 1 CenaZ_VT, CenaZ_NT, CenaP_VT, CenaP_NT, CenaC_VT, CenaC_NT 
                      FROM TarifniModeli WHERE IsAktivan = 1";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return 0;

            decimal zbir = 0;
            for (int i = 0; i < 6; i++) zbir += reader.GetDecimal(i);
            return zbir / 6;
        }

        private async Task PosaljiMejlUpozorenje(string email, string brojiloId, decimal potrosnja, decimal trenutnaVrednost, decimal limit, bool jeRSD)
        {
            try
            {
                string sendGridKey = Environment.GetEnvironmentVariable("SendGridApiKey");
                var sgClient = new SendGridClient(sendGridKey);

                string jedinicaTekst = jeRSD ? "RSD" : "kWh";

                var poruka = new SendGridMessage
                {
                    From = new EmailAddress("noreply@smartmetering.rs", "Smart Metering"),
                    Subject = "Upozorenje: Prekoracen limit potrosnje struje",
                    PlainTextContent = $"Postovani,\n\nVasa potrosnja za brojilo {brojiloId} u ovom mesecu iznosi {potrosnja:F2} kWh " +
                                       $"({trenutnaVrednost:F2} {jedinicaTekst}), sto prekoracuje Vas postavljeni limit od {limit:F2} {jedinicaTekst}.\n\n" +
                                       $"Smart Metering tim."
                };
                poruka.AddTo(new EmailAddress(email));

                await sgClient.SendEmailAsync(poruka);
                _logger.LogInformation($"[LIMIT] Email upozorenje poslat na {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[LIMIT] Greska pri slanju mejla upozorenja: {ex.Message}");
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