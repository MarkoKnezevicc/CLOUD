using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FunctionApp
{
    public class GetNepotvrdjenaMerenja
    {
        [Function("GetNepotvrdjenaMerenja")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "validacija/merenja/na-cekanju")] HttpRequest req)
        {
            try
            {
                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                // --- DODATO: Podešavanje CORS-a za Azurite Blob Storage ---
                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                try
                {
                    var serviceProperties = await blobServiceClient.GetPropertiesAsync();
                    serviceProperties.Value.Cors.Clear();
                    serviceProperties.Value.Cors.Add(new BlobCorsRule
                    {
                        AllowedOrigins = "*", // U produkciji stavi tačan URL frontend-a (npr. http://localhost:5173)
                        AllowedMethods = "GET,POST,OPTIONS,PUT",
                        AllowedHeaders = "*",
                        ExposedHeaders = "*",
                        MaxAgeInSeconds = 3600
                    });
                    await blobServiceClient.SetPropertiesAsync(serviceProperties.Value);
                }
                catch (Exception) { /* Ignoriši ako Azurite privremeno zaključa svojstva */ }
                // --------------------------------------------------------

                var tableClient = new TableClient(storageConnectionString, "NepotvrdjenaMerenja");
                await tableClient.CreateIfNotExistsAsync();

                var lista = new List<object>();
                var entities = tableClient.QueryAsync<TableEntity>();

                await foreach (var entity in entities)
                {
                    lista.Add(new
                    {
                        BrojiloId = entity.PartitionKey,
                        SlikaIme = entity.RowKey,
                        Vrednost = entity.GetDouble("Vrednost"),
                        Status = entity.GetString("Status"),
                        DatumUnosa = entity.GetDateTime("DatumUnosa"),
                        SlikaUrl = $"http://127.0.0.1:10000/devstoreaccount1/rucni-unosi-original/{entity.RowKey}"
                    });
                }

                return new OkObjectResult(lista);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { greska = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}