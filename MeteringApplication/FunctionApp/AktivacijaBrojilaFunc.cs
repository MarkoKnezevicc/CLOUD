using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace SmartMetering.AzureFunctions
{
    public class AktivacijaBrojilaFunc
    {
        private readonly ILogger _logger;

        public AktivacijaBrojilaFunc(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AktivacijaBrojilaFunc>();
        }

        [Function("AktivirajBrojilo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "device/activate")] HttpRequestData req)
        {
            _logger.LogInformation("--- .NET ISOLATED AZURE FUNCTION: Pokrenut Handshake protokol ---");

            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                _logger.LogCritical("[CRITICAL] 'SqlConnectionString' nije pronađen u konfiguraciji!");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Konfiguraciona greška. Konekcioni string nedostaje na serveru." }));
                return response;
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<AktivacijaZahtevDto>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.SerijskiBroj) || string.IsNullOrEmpty(data.Uuid))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "SerijskiBroj i Uuid su obavezni parametri." }));
                return response;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnectionString))
                {
                    await conn.OpenAsync();

                    // PROVERA 1: Da li ovaj specifični hardverski UUID već drži NEKO DRUGO brojilo?
                    string uuidProveraQuery = "SELECT SerijskiBroj FROM PametnaBrojila WHERE Uuid = @Uuid";
                    using (SqlCommand uuidCmd = new SqlCommand(uuidProveraQuery, conn))
                    {
                        uuidCmd.Parameters.AddWithValue("@Uuid", data.Uuid);
                        object postojeciSerijski = await uuidCmd.ExecuteScalarAsync();

                        if (postojeciSerijski != null)
                        {
                            response.StatusCode = HttpStatusCode.BadRequest;
                            await response.WriteStringAsync(JsonConvert.SerializeObject(new
                            {
                                poruka = $"Ovaj IoT hardver (UUID) je već uparen sa brojilom {postojeciSerijski}. Uređaj ne može držati dva brojila!"
                            }));
                            return response;
                        }
                    }

                    // PROVERA 2: Provera postojanja i statusa samog brojila na osnovu serijskog broja
                    string proveraQuery = "SELECT Id, Status FROM PametnaBrojila WHERE SerijskiBroj = @SerijskiBroj";
                    int brojiloId = 0;
                    int trenutniStatus = -1;

                    using (SqlCommand checkCmd = new SqlCommand(proveraQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@SerijskiBroj", data.SerijskiBroj);
                        using (SqlDataReader reader = await checkCmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                brojiloId = reader.GetInt32(0);
                                trenutniStatus = reader.GetInt32(1);
                            }
                        }
                    }

                    if (brojiloId == 0)
                    {
                        response.StatusCode = HttpStatusCode.NotFound;
                        await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Brojilo sa ovim serijskim brojem nije registrovano na platformi." }));
                        return response;
                    }

                    if (trenutniStatus != 0)
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Ovo pametno brojilo je već aktivirano i upareno u bazi podataka." }));
                        return response;
                    }

                    // GENERISANJE ACCESS TOKENA
                    string deviceAccessToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

                    string azurirajQuery = @"UPDATE PametnaBrojila 
                                             SET Uuid = @Uuid, DeviceAccessToken = @Token, Status = 1 
                                             WHERE Id = @Id";

                    using (SqlCommand updateCmd = new SqlCommand(azurirajQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@Uuid", data.Uuid);
                        updateCmd.Parameters.AddWithValue("@Token", deviceAccessToken);
                        updateCmd.Parameters.AddWithValue("@Id", brojiloId);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation($"[SUCCESS] Brojilo {data.SerijskiBroj} uspešno aktivirano!");

                    response.StatusCode = HttpStatusCode.OK;
                    var uspesanOdgovor = new
                    {
                        poruka = "Aktivacija uspešna! Uređaj je uspešno uparen.",
                        deviceAccessToken = deviceAccessToken
                    };

                    await response.WriteStringAsync(JsonConvert.SerializeObject(uspesanOdgovor));
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Greška pri radu sa bazom: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = $"Sistemska greška na serveru: {ex.Message}" }));
                return response;
            }
        }
    }

    public class AktivacijaZahtevDto
    {
        public string SerijskiBroj { get; set; }
        public string Uuid { get; set; }
    }
}