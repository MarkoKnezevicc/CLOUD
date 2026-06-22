using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Blobs;
using Azure.Data.Tables;

namespace FunctionApp
{
    public class PrijaviMerenjeHttp
    {
        [Function("PrijaviMerenje")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "merenje/prijavi")] HttpRequest req)
        {
            try
            {
                var form = await req.ReadFormAsync();

                string brojiloId = form["BrojiloId"];
                string vrednost = form["Vrednost"];
                var file = form.Files.GetFile("Slika") ?? (form.Files.Count > 0 ? form.Files[0] : null);

                if (string.IsNullOrEmpty(brojiloId) || string.IsNullOrEmpty(vrednost) || file == null)
                {
                    return new BadRequestObjectResult("Nedostaju obavezni parametri (BrojiloId, Vrednost ili Slika).");
                }

                string jedinstvenoIme = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("rucni-unosi-original");
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(jedinstvenoIme);
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                var tableClient = new TableClient(storageConnectionString, "NepotvrdjenaMerenja");
                await tableClient.CreateIfNotExistsAsync();

                if (!double.TryParse(vrednost, out double doubleVrednost))
                {
                    return new BadRequestObjectResult("Vrednost mora biti broj.");
                }

                var entity = new TableEntity(brojiloId, jedinstvenoIme)
                {
                    { "Vrednost", doubleVrednost },
                    { "Status", "NaCekanju" },
                    { "DatumUnosa", DateTime.UtcNow }
                };
                await tableClient.AddEntityAsync(entity);

                return new OkObjectResult(new { poruka = "Merenje uspesno poslato na cekanje." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { greska = ex.Message, detalji = ex.InnerException?.Message }) { StatusCode = 500 };
            }
        }
    }
}