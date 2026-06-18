using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BrojiloSimulator
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        //url
        private const string AzureFuncBaseUrl = "http://localhost:7056/api";
        private static string AzureActivateUrl => $"{AzureFuncBaseUrl}/device/activate";
        private static string AzureTelemetryUrl => $"{AzureFuncBaseUrl}/telemetrija";

        static async Task Main(string[] args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=========================================================");
                Console.WriteLine("   PAMETNO BROJILO (SIMULATOR HARDVERA) - ONLINE   ");
                Console.WriteLine("=========================================================");
                Console.ResetColor();

                // 1. Unos serijskog broja odmah na početku
                Console.Write("Unesite serijski broj brojila (Format: SA-YYYY-XXXXX): ");
                string serijskiBroj = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(serijskiBroj))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Greška: Serijski broj ne može biti prazan!");
                    Console.ResetColor();
                    return;
                }

                // Naziv fajla je sada direktno vezan za serijski broj brojila
                string configFajlIme = $"{serijskiBroj}.txt";

                string deviceUuid = "";
                string deviceAccessToken = "";

                // 2. LOGIKA PROVERE: Da li u folderu već postoji par sa ovim serijskim brojem?
                if (File.Exists(configFajlIme))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nPronađen lokalni par za brojilo {serijskiBroj}!");
                    Console.ResetColor();

                    // Čitamo sačuvane podatke o uređaju
                    string fajlSadrzaj = File.ReadAllText(configFajlIme);
                    var sacuvaniPodaci = JsonConvert.DeserializeAnonymousType(fajlSadrzaj, new { Uuid = "", Token = "" });

                    deviceUuid = sacuvaniPodaci?.Uuid;
                    deviceAccessToken = sacuvaniPodaci?.Token;

                    Console.WriteLine($"[HARDWARE] Učitan fabrički UUID: {deviceUuid}");
                    Console.WriteLine($"[INFO] Uređaj je preskočio aktivaciju jer je već uparen.");
                }
                else
                {
                    // AKO NE POSTOJI: Generiše se potpuno novi hardverski identitet (UUID) za ovo novo brojilo
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nPar za brojilo {serijskiBroj} ne postoji lokalno. Pokrećem uparivanje...");
                    Console.ResetColor();

                    deviceUuid = Guid.NewGuid().ToString();
                    Console.WriteLine($"[HARDWARE] Izgenerisan novi UUID uređaja: {deviceUuid}");

                    // Pokretanje trostepenog Handshake-a ka Azure Cloud-u
                    deviceAccessToken = await PokreniHandshakeAktivaciju(serijskiBroj, deviceUuid);

                    if (!string.IsNullOrEmpty(deviceAccessToken))
                    {
                        // Pakujemo oba podatka zajedno i pravimo trajni par u fajlu koji se zove kao serijski broj
                        var podaciZaSnimanje = new { Uuid = deviceUuid, Token = deviceAccessToken };
                        string jsonZaSnimanje = JsonConvert.SerializeObject(podaciZaSnimanje, Formatting.Indented);

                        File.WriteAllText(configFajlIme, jsonZaSnimanje);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Uređaj je uspešno uparen! Konfiguracioni fajl '{configFajlIme}' je kreiran.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Aktivacija odbijena od strane Azure Cloud-a. Zaustavljam simulator.");
                        Console.ResetColor();
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(deviceAccessToken))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("\nSTATUS: Brojilo je u aktivnom stanju rada i povezano je na mrežu.");
                    Console.WriteLine($"[AUTH TOKEN]: {deviceAccessToken.Substring(0, Math.Min(10, deviceAccessToken.Length))}*********************");
                    Console.ResetColor();
                }

                if (!string.IsNullOrEmpty(deviceAccessToken))
                {
                    await PokreniSimulacijuTelemetrije(serijskiBroj, deviceAccessToken);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nGreška u radu simulatora: {ex.Message}");
                Console.ResetColor();
            }


            
        }

        private static async Task<string> PokreniHandshakeAktivaciju(string serijskiBroj, string uuid)
        {
            Console.WriteLine($"[HANDSHAKE] Slanje mrežnog paketa na Cloud Gateway ({AzureActivateUrl})...");

            var payload = new { SerijskiBroj = serijskiBroj, Uuid = uuid };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(AzureActivateUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[SERVER RESPONSE] HTTP mrežni status: {(int)response.StatusCode} {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var resData = JsonConvert.DeserializeAnonymousType(responseString, new { deviceAccessToken = "" });
                    return resData?.deviceAccessToken;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ODBIJENO] Razlog sa servera: {responseString}");
                    Console.ResetColor();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nMREŽNA GREŠKA: Konekcija sa Azure funkcijom nije uspela! {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }


        private static async Task PokreniSimulacijuTelemetrije(string serijskiBroj, string token)
        {
            bool jeMonofazno = serijskiBroj.StartsWith("SM-", StringComparison.OrdinalIgnoreCase);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\nPOKRENUT REŽIM REAL-TIME TELEMETRIJE!");
            Console.WriteLine($"[HARDWARE TIP]: {(jeMonofazno ? "MONOFAZNO BROJILO" : "TROFAZNO BROJILO")}");
            Console.ResetColor();

            Random rand = new Random();
            decimal ukupnaPotrosnjaKwh = (decimal)(rand.NextDouble() * 500 + 2100);
            DateTime virtuelnoVreme = DateTime.Now;

            

            while (true)
            {
                bool simulirajPadNapona = rand.Next(1, 101) <= 15;
                // Objekat koji šaljemo ka Azure funkciji kolege
                object telemetrijaDto = null;

                if (jeMonofazno)
                {
                    decimal napon;
                    //1. LOGIKA ZA MONOFAZNO BROJILO (Sve faze L1, L2, L3 ostaju NULL)
                    if (simulirajPadNapona)
                    {
                        // Pad napona: generiše vrednost između 165.0V i 188.9V (ispod 190V)
                        napon = (decimal)(165.0 + rand.NextDouble() * 24);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[ KVAR - MONOFAZNO] Veštački izazvan pad napona: {Math.Round(napon, 1)}V!");
                        Console.ResetColor();
                    }
                    else
                    {
                        // Normalno stanje (tvoj originalni kod)
                        napon = (decimal)(229.0 + rand.NextDouble() * 3);
                    }
                    decimal struja = (decimal)(0.5 + rand.NextDouble() * 15); // Jedna strujna linija
                    decimal cosPhi = (decimal)(0.90 + rand.NextDouble() * 0.08);

                    // P = (U * I * cosPhi) / 1000
                    decimal trenutnoOpterecenjeKw = (napon * struja * cosPhi) / 1000;
                    ukupnaPotrosnjaKwh += trenutnoOpterecenjeKw;

                    telemetrijaDto = new
                    {
                        VremeMerenja = virtuelnoVreme,
                        UkupnaPotrosnja = Math.Round(ukupnaPotrosnjaKwh, 4),
                        TrenutnoOpterecenje = Math.Round(trenutnoOpterecenjeKw, 3),

                        
                        Napon = Math.Round(napon, 1),
                        Struja = Math.Round(struja, 2),
                        FaktorSnage = Math.Round(cosPhi, 2),

                       
                        NaponL1 = (decimal?)null,
                        NaponL2 = (decimal?)null,
                        NaponL3 = (decimal?)null,
                        StrujaL1 = (decimal?)null,
                        StrujaL2 = (decimal?)null,
                        StrujaL3 = (decimal?)null,
                        FaktorSnageL1 = (decimal?)null,
                        FaktorSnageL2 = (decimal?)null,
                        FaktorSnageL3 = (decimal?)null
                    };
                }
                else
                {
                   
                    decimal naponL1 = (decimal)(228.0 + rand.NextDouble() * 4);
                    decimal naponL2 = (decimal)(227.5 + rand.NextDouble() * 4);
                    decimal naponL3 = (decimal)(229.0 + rand.NextDouble() * 4);

                    decimal strujaL1 = (decimal)(1.0 + rand.NextDouble() * 12);
                    decimal strujaL2 = (decimal)(0.8 + rand.NextDouble() * 14);
                    decimal strujaL3 = (decimal)(1.2 + rand.NextDouble() * 11);

                    decimal cosPhiL1 = (decimal)(0.88 + rand.NextDouble() * 0.08);
                    decimal cosPhiL2 = (decimal)(0.86 + rand.NextDouble() * 0.09);
                    decimal cosPhiL3 = (decimal)(0.89 + rand.NextDouble() * 0.07);

                    decimal snagaL1 = (naponL1 * strujaL1 * cosPhiL1) / 1000;
                    decimal snagaL2 = (naponL2 * strujaL2 * cosPhiL2) / 1000;
                    decimal snagaL3 = (naponL3 * strujaL3 * cosPhiL3) / 1000;
                    decimal trenutnoOpterecenjeKw = snagaL1 + snagaL2 + snagaL3;

                    ukupnaPotrosnjaKwh += trenutnoOpterecenjeKw;

                    telemetrijaDto = new
                    {
                        VremeMerenja = virtuelnoVreme,
                        UkupnaPotrosnja = Math.Round(ukupnaPotrosnjaKwh, 4),
                        TrenutnoOpterecenje = Math.Round(trenutnoOpterecenjeKw, 3),

                        // Sumarni prosek
                        Napon = Math.Round((naponL1 + naponL2 + naponL3) / 3, 1),
                        Struja = Math.Round(strujaL1 + strujaL2 + strujaL3, 2),
                        FaktorSnage = Math.Round((cosPhiL1 + cosPhiL2 + cosPhiL3) / 3, 2),

                        // Kompletne faze
                        NaponL1 = Math.Round(naponL1, 1),
                        NaponL2 = Math.Round(naponL2, 1),
                        NaponL3 = Math.Round(naponL3, 1),
                        StrujaL1 = Math.Round(strujaL1, 2),
                        StrujaL2 = Math.Round(strujaL2, 2),
                        StrujaL3 = Math.Round(strujaL3, 2),
                        FaktorSnageL1 = Math.Round(cosPhiL1, 2),
                        FaktorSnageL2 = Math.Round(cosPhiL2, 2),
                        FaktorSnageL3 = Math.Round(cosPhiL3, 2)
                    };
                }

                // Slanje zahtjeva na Azure funkciju
                string jsonBody = JsonConvert.SerializeObject(telemetrijaDto);
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AzureTelemetryUrl))
                {
                    requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    requestMessage.Headers.Add("X-Device-Token", token);

                    try
                    {
                        HttpResponseMessage response = await client.SendAsync(requestMessage);
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var resObj = JsonConvert.DeserializeAnonymousType(responseText, new { poruka = "", tarifa = "" });
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Poslato ({(jeMonofazno ? "1-Faza" : "3-Faze")}) -> Potrošnja: {JsonConvert.DeserializeAnonymousType(jsonBody, new { UkupnaPotrosnja = 0.0m }).UkupnaPotrosnja} kWh | Tarifa: {resObj?.tarifa}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\nGreška: {response.StatusCode} | {responseText}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nMrežna greška: {ex.Message}");
                    }
                }

                Console.ResetColor();
                virtuelnoVreme = virtuelnoVreme.AddHours(1);
                await Task.Delay(5000);
            }
        }


    }
}