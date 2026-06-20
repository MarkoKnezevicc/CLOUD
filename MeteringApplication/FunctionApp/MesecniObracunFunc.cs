using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Azure.Data.Tables;
using System.Linq;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FunctionApp
{
    public class MesecniObracunFunc
    {
        private readonly ILogger _logger;
        public MesecniObracunFunc(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MesecniObracunFunc>();
        }

        // Menjam timer trigger cisto radi testiranja
        // Za potrebe testiranja -> [TimerTrigger("0 */2 * * * *")] TimerInfo timerInfo)

        [Function("MesecniObracun")]
        public async Task Run(
            //[TimerTrigger("0 0 0 1 * *")] TimerInfo timerInfo)
            [TimerTrigger("0 * * * * *")] TimerInfo timerInfo)
        {
            _logger.LogInformation($"[OBRACUN] Mesecni obracun pokrenut: {DateTime.UtcNow}");

            string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            DateTime prosliMesec = DateTime.UtcNow;
            int godina = prosliMesec.Year;
            int mesec = prosliMesec.Month;

            _logger.LogInformation($"[OBRACUN] Obradjujem period: {mesec} /{ godina}");

            // Logika ovde
            using SqlConnection conn = new SqlConnection(sqlConnectionString);
            await conn.OpenAsync();

            // Ucitaj aktivni tarifni model
            decimal cenaZ_VT = 0, cenaZ_NT = 0;
            decimal cenaP_VT = 0, cenaP_NT = 0;
            decimal cenaC_VT = 0, cenaC_NT = 0;
            decimal cenaObracunskeSnage = 0, trosakSnabdevaca = 0;

            string tarifaQuery = @"SELECT TOP 1 CenaZ_VT, CenaZ_NT, CenaP_VT, CenaP_NT, 
                        CenaC_VT, CenaC_NT, CenaObracunskeSnage, TrosakSnabdevaca 
                        FROM TarifniModeli WHERE IsAktivan = 1";

            using (SqlCommand cmd = new SqlCommand(tarifaQuery, conn))
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.Read())
                {
                    _logger.LogError("[OBRACUN] Nema aktivnog tarifnog modela! Obracun se prekida.");
                    return;
                }

                cenaZ_VT = reader.GetDecimal(0);
                cenaZ_NT = reader.GetDecimal(1);
                cenaP_VT = reader.GetDecimal(2);
                cenaP_NT = reader.GetDecimal(3);
                cenaC_VT = reader.GetDecimal(4);
                cenaC_NT = reader.GetDecimal(5);
                cenaObracunskeSnage = reader.GetDecimal(6);
                trosakSnabdevaca = reader.GetDecimal(7);
            }

            _logger.LogInformation("[OBRACUN] Aktivni tarifni model ucitan.");

            // Ucitavanje uparenih brojila

            var brojilaLista = new List<(int Id, int KorisnikId, decimal Snaga, string Email)>();

            string brojilaQuery = @"SELECT pb.Id, o.KorisnikId, pb.MaksimalnaOdobrenaSnaga, k.Email
                         FROM PametnaBrojila pb
                         INNER JOIN Objekti o ON o.Id = pb.ObjekatId
                         INNER JOIN Korisnici k ON k.Id = o.KorisnikId
                         WHERE pb.Status = 1";

            using (SqlCommand cmd = new SqlCommand(brojilaQuery, conn))
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    brojilaLista.Add((
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetDecimal(2),
                        reader.GetString(3)
                    ));
                }
            }

            _logger.LogInformation($"[OBRACUN] Pronadjeno {brojilaLista.Count} uparenih brojila.");

            // Racunanje potrosnje iz Table Storage-a za svako brojilo

            string tableConnString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(tableConnString, "Telemetrije");
            tableClient.CreateIfNotExists(); 

            foreach (var brojilo in brojilaLista)
            {
                // Filtriramo telemetriju samo za prethnodni mesec i za ovo brojilo
                string filter = $"PartitionKey eq '{brojilo.Id}'";

                // Ucitavamo sva merenja za za mesec

                var merenja = new List<(decimal UkupnaPotrosnja, string Tarifa, DateTime vreme)>();

                await foreach (var entitet in tableClient.QueryAsync<TableEntity>(filter))
                {
                    var vremeOffset = entitet.GetDateTimeOffset("VremeMerenja");
                    if (vremeOffset == null) continue;
                    DateTime vreme = vremeOffset.Value.UtcDateTime;

                    // Filtriramo samo prethodni mesec
                    if (vreme.Year != godina || vreme.Month != mesec) continue;

                    decimal potrosnja = entitet.GetDouble("UkupnaPotrosnja") is double d ? (decimal)d : 0;
                    string tarifa = entitet.GetString("Tarifa") ?? "";
                    merenja.Add((potrosnja, tarifa, vreme));
                }

                // Sortiramo po redosledu kako su stigli (RowKey je timestamp)
                merenja = merenja.OrderBy(m => m.UkupnaPotrosnja).ToList();

                decimal ukupnoVT = 0;
                decimal ukupnoNT = 0;

                // Razlika izmedju uzastopnih merenja + tarifa
                for (int i = 1; i < merenja.Count; i++)
                {
                    decimal delta = merenja[i].UkupnaPotrosnja - merenja[i - 1].UkupnaPotrosnja;
                    if (delta < 0) delta = 0; // Da ne budu negativne vrednosti

                    if (merenja[i].Tarifa == "VisaTarifa")
                        ukupnoVT += delta;
                    else
                        ukupnoNT += delta;
                }

                _logger.LogInformation($"[OBRACUN] Brojilo {brojilo.Id}: {merenja.Count} merenja, VT = {ukupnoVT}, NT = {ukupnoNT}");

                if (merenja.Count < 2)
                {
                    _logger.LogWarning($"[OBRACUN] Brojilo {brojilo.Id}: nema dovoljno merenja, preskacem.");
                    continue;
                }

                // Formula za obracun ovde

                decimal eUkupno = ukupnoVT + ukupnoNT;
                if (eUkupno == 0) { continue; }

                decimal kVT = ukupnoVT / eUkupno;
                decimal kNT = ukupnoNT / eUkupno;

                // Zone
                decimal zUkupno = Math.Min(eUkupno, 350);
                decimal pUkupno = Math.Max(0, Math.Min(eUkupno - 350, 850));
                decimal cUkupno = Math.Max(0, eUkupno - 1200);

                // Raspodela VT/NT po zonama
                decimal zVT = zUkupno * kVT; decimal zNT = zUkupno * kNT;
                decimal pVT = pUkupno * kVT; decimal pNT = pUkupno * kNT;
                decimal cVT = cUkupno* kVT; decimal cNT = cUkupno * kNT;

                // Finansijski iznosi
                decimal iznosZelena = (zVT * cenaZ_VT) + (zNT * cenaZ_NT);
                decimal iznosPlava = (pVT * cenaP_VT) + (pNT * cenaP_NT);
                decimal iznosCrvena = (cVT * cenaC_VT) + (cNT * cenaC_NT);

                // Fiksni troskovi
                decimal fiksniTroskovi = (brojilo.Snaga * cenaObracunskeSnage) + trosakSnabdevaca;

                decimal ukupanIznos = iznosZelena + iznosPlava + iznosCrvena + fiksniTroskovi;

                _logger.LogInformation($"[OBRACUN] Brojilo {brojilo.Id}: Ukupan iznos = {ukupanIznos:F2} RSD");

                // Generisanje teksta racuna

                string tekstRacuna = $"""
                    ========================================
                    RACUN ZA ELEKTRICNU ENERGIJU
                    ========================================
                    Period: {mesec:D2}/{godina}
                    Datum izdavanja: {DateTime.UtcNow:dd.MM.yyyy}
                    Brojilo ID: {brojilo.Id}
                    ----------------------------------------
                    POTROŠNJA:
                    Viša tarifa  (VT): {ukupnoVT:F4} kWh
                    Niža tarifa  (NT): {ukupnoNT:F4} kWh
                    Ukupno:            {eUkupno:F4} kWh
                    ----------------------------------------
                    ZONE:
                    Zelena (0-350 kWh):    {iznosZelena:F2} RSD
                    Plava  (351-1200 kWh): {iznosPlava:F2} RSD
                    Crvena (>1200 kWh):    {iznosCrvena:F2} RSD
                    Fiksni troškovi:       {fiksniTroskovi:F2} RSD
                    ----------------------------------------
                    UKUPAN IZNOS: {ukupanIznos:F2} RSD
                    ========================================
                    """;

                // Provera da li racun vec postoji za ovaj mesec i ovo brojilo
                string proveraQuery = "SELECT COUNT(1) FROM Racuni WHERE BrojiloId = @BId AND GodinaObracuna = @God AND MesecObracuna = @Mes";
                using (SqlCommand proveraCmd = new SqlCommand(proveraQuery, conn))
                {
                    proveraCmd.Parameters.AddWithValue("@BId", brojilo.Id);
                    proveraCmd.Parameters.AddWithValue("@God", godina);
                    proveraCmd.Parameters.AddWithValue("@Mes", mesec);
                    int count = (int)await proveraCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        _logger.LogInformation($"[OBRACUN] Racun za brojilo {brojilo.Id} za {mesec}/{godina} vec postoji, preskacem.");
                        continue;
                    }
                }

                // Upis racuna u SQL bazu
                string insertQuery = @"INSERT INTO Racuni 
                    (BrojiloId, KorisnikId, GodinaObracuna, MesecObracuna,
                     EnergijaVT, EnergijaNT, IznosZelena, IznosPlava, IznosCrvena,
                     FiksniTroskovi, UkupanIznos, Status, DatumIzdavanja, TekstRacuna)
                    VALUES
                    (@BrojiloId, @KorisnikId, @Godina, @Mesec,
                     @VT, @NT, @Zelena, @Plava, @Crvena,
                     @Fiksni, @Ukupno, 0, @Datum, @Tekst)";

                using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@BrojiloId", brojilo.Id);
                insertCmd.Parameters.AddWithValue("@KorisnikId", brojilo.KorisnikId);
                insertCmd.Parameters.AddWithValue("@Godina", godina);
                insertCmd.Parameters.AddWithValue("@Mesec", mesec);
                insertCmd.Parameters.AddWithValue("@VT", ukupnoVT);
                insertCmd.Parameters.AddWithValue("@NT", ukupnoNT);
                insertCmd.Parameters.AddWithValue("@Zelena", iznosZelena);
                insertCmd.Parameters.AddWithValue("@Plava", iznosPlava);
                insertCmd.Parameters.AddWithValue("@Crvena", iznosCrvena);
                insertCmd.Parameters.AddWithValue("@Fiksni", fiksniTroskovi);
                insertCmd.Parameters.AddWithValue("@Ukupno", ukupanIznos);
                insertCmd.Parameters.AddWithValue("@Datum", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@Tekst", tekstRacuna);

                await insertCmd.ExecuteNonQueryAsync();

                _logger.LogInformation($"[OBRACUN] Racun za brojilo {brojilo.Id} uspesno sacuvan.");

                // Slanje email-a potrosacu

                try
                {
                    string sendGridKey = Environment.GetEnvironmentVariable("SendGridApiKey");

                    var sgClient = new SendGridClient(sendGridKey);

                    var poruka = new SendGridMessage
                    {
                        From = new EmailAddress("noreply@smartmetering.rs", "Smart Metering"),
                        Subject = $"Vas racun za {mesec:D2}/{godina}",
                        PlainTextContent = tekstRacuna
                    };
                    poruka.AddTo(new EmailAddress(brojilo.Email));

                    await sgClient.SendEmailAsync(poruka);
                    _logger.LogInformation($"[OBRACUN] Email poslat na {brojilo.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[OBRACUN] Greska pri slanju mejla: {ex.Message}");
                }
            }
        }
    }
}
