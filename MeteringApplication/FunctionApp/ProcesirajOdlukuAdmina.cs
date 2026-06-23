using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SmartMetering.AzureFunctions.Domain;
using System;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class ProcesirajOdlukuAdmina
    {
        private readonly ITelemetrijaRepository _telemetrijaRepository;

        public ProcesirajOdlukuAdmina(ITelemetrijaRepository telemetrijaRepository)
        {
            _telemetrijaRepository = telemetrijaRepository;
        }

        [Function("ProcesirajOdlukuAdmina")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "validacija/merenja/odluka")] HttpRequest req)
        {
            try
            {
                string brojiloId = req.Query["brojiloId"];
                string slikaIme = req.Query["slikaIme"];
                string akcija = req.Query["akcija"];

                if (string.IsNullOrEmpty(brojiloId) || string.IsNullOrEmpty(slikaIme) || string.IsNullOrEmpty(akcija))
                {
                    return new BadRequestObjectResult("Nedostaju query parametri: brojiloId, slikaIme ili akcija.");
                }

                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var privremenaTabela = new TableClient(storageConnectionString, "NepotvrdjenaMerenja");

                var response = await privremenaTabela.GetEntityAsync<TableEntity>(brojiloId, slikaIme);
                var privremenoMerenje = response.Value;

                if (akcija.ToLower() == "odobri")
                {
                    double doubleVrednost = 0;
                    if (privremenoMerenje.TryGetValue("Vrednost", out object sirovaVrednost))
                    {
                        if (sirovaVrednost is double d) doubleVrednost = d;
                        else if (sirovaVrednost is int i) doubleVrednost = i;
                        else if (sirovaVrednost is long l) doubleVrednost = l;
                        else doubleVrednost = Convert.ToDouble(sirovaVrednost);
                    }

                    var telemetrija = new Telemetrija
                    {
                        Id = Guid.NewGuid(),
                        BrojiloId = int.Parse(brojiloId),
                        VremeMerenja = DateTime.UtcNow,
                        UkupnaPotrosnja = (decimal)doubleVrednost,
                        TrenutnoOpterecenje = 0,
                        Tarifa = TipTarife.NizaTarifa, 
                        Napon = null,
                        Struja = null,
                        FaktorSnage = null,
                        NaponL1 = null,
                        NaponL2 = null,
                        NaponL3 = null,
                        StrujaL1 = null,
                        StrujaL2 = null,
                        StrujaL3 = null,
                        FaktorSnageL1 = null,
                        FaktorSnageL2 = null,
                        FaktorSnageL3 = null
                    };
                    var blobServiceClient = new BlobServiceClient(storageConnectionString);
                    await _telemetrijaRepository.SaveAsync(telemetrija);
                    var originalContainer = blobServiceClient.GetBlobContainerClient("rucni-unosi-original");
                    await originalContainer.GetBlobClient(slikaIme).DeleteIfExistsAsync();
                }
                else if (akcija.ToLower() == "odbij")
                {
                    var blobServiceClient = new BlobServiceClient(storageConnectionString);
                    var originalContainer = blobServiceClient.GetBlobContainerClient("rucni-unosi-original");
                    await originalContainer.GetBlobClient(slikaIme).DeleteIfExistsAsync();
                }

                await privremenaTabela.DeleteEntityAsync(brojiloId, slikaIme);

                return new OkObjectResult(new { poruka = $"Merenje uspešno obrađeno akcijom: {akcija}." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { greska = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}