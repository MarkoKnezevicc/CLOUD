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
        private const string AzureFuncUrl = "http://localhost:7056/api/device/activate";

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

                // 3. UREĐAJ JE SPREMAN ZA RAD (UPAREN ILI UČITAN)
                if (!string.IsNullOrEmpty(deviceAccessToken))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("\nSTATUS: Brojilo je u aktivnom stanju rada i povezano je na mrežu.");
                    Console.WriteLine($"[AUTH TOKEN]: {deviceAccessToken.Substring(0, Math.Min(10, deviceAccessToken.Length))}*********************");
                    Console.ResetColor();

                    // Ovde će u budućnosti ići petlja za periodično slanje telemetrije
                    Console.WriteLine("Sistem je spreman. Slanje telemetrije je trenutno isključeno u ovom koraku.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n💥 Greška u radu simulatora: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n[SISTEM] Pritisnite bilo koji taster za zatvaranje...");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        private static async Task<string> PokreniHandshakeAktivaciju(string serijskiBroj, string uuid)
        {
            Console.WriteLine($"[HANDSHAKE] Slanje mrežnog paketa na Cloud Gateway ({AzureFuncUrl})...");

            var payload = new { SerijskiBroj = serijskiBroj, Uuid = uuid };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(AzureFuncUrl, content);
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
                Console.WriteLine($"\n💥 MREŽNA GREŠKA: Konekcija sa Azure funkcijom nije uspela! {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }
    }
}