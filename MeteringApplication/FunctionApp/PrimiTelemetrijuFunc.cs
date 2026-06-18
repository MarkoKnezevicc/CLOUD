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

        public PrimiTelemetrijuFunc(ILoggerFactory loggerFactory, IBrojiloRepository brojiloRepository)
        {
            _logger = loggerFactory.CreateLogger<PrimiTelemetrijuFunc>();
            _brojiloRepository = brojiloRepository;
        }

        // Visestruki izlaz: vraca 202 Accepted simulatoru i ubacuje upakovani paket u Azure Queue
        public class VisestrukiIzlaz
        {
            public HttpResponseData HttpResponse { get; set; }

            [QueueOutput("telemetrija-queue", Connection = "AzureWebJobsStorage")]
            public string QueueMessage { get; set; }
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
                return new VisestrukiIzlaz { HttpResponse = httpResponse, QueueMessage = null };
            }

            string deviceToken = tokenValues.FirstOrDefault() ?? string.Empty;

            // 2. Pronalaženje brojila u SQL bazi na osnovu poslatog tokena (Brza provera)
            var brojilo = await _brojiloRepository.GetByDeviceTokenAsync(deviceToken);
            if (brojilo == null)
            {
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new { poruka = "Nevažeći Device Access Token." }));
                return new VisestrukiIzlaz { HttpResponse = httpResponse, QueueMessage = null };
            }

            // 3. Čitanje sirovog JSON tela sa simulatora
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // 4. Pakujemo podatke o brojilu i sirovi DTO zajedno u jedan tranzitni paket za Queue
            var tranzitniPaket = new
            {
                BrojiloId = brojilo.Id.ToString(), 
                SirovaTelemetrijaJson = requestBody
            };

            string queuePayload = JsonConvert.SerializeObject(tranzitniPaket);

            // Formiranje uspešnog HTTP odgovora (202 Accepted - Primljeno na obradu u pozadini)
            httpResponse.StatusCode = HttpStatusCode.Accepted;
            await httpResponse.WriteStringAsync(JsonConvert.SerializeObject(new
            {
                poruka = "Telemetrija primljena i prosleđena na asinhranu obradu."
            }));

            _logger.LogInformation($"[QUEUE INGESTION] Uspešno upisana poruka u red za brojilo: {brojilo.Id}");

            return new VisestrukiIzlaz
            {
                HttpResponse = httpResponse,
                QueueMessage = queuePayload //Azure automatski salje ovo u telemetrija-queue
            };
        }
    }
}