using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Stripe;
using Stripe.Checkout;

namespace FunctionApp
{
    public class StripeWebhookFunc
    {
        private readonly ILogger _logger;

        public StripeWebhookFunc(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StripeWebhookFunc>();
        }

        [Function("StripeWebhook")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stripe-webhook")] HttpRequestData req)
        {
            string json = await new StreamReader(req.Body).ReadToEndAsync();
            string webhookSecret = Environment.GetEnvironmentVariable("StripeWebhookSecret");

            Event stripeEvent;
            try
            {
                string signatureHeader = req.Headers.GetValues("Stripe-Signature").FirstOrDefault();
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[STRIPE WEBHOOK] Neispravan potpis: {ex.Message}");
                var greska = req.CreateResponse(HttpStatusCode.BadRequest);
                return greska;
            }

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.Metadata.TryGetValue("racunId", out string racunIdStr))
                {
                    int racunId = int.Parse(racunIdStr);

                    string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                    using SqlConnection conn = new SqlConnection(sqlConnectionString);
                    await conn.OpenAsync();

                    string updateQuery = "UPDATE Racuni SET Status = 1 WHERE Id = @RacunId";
                    using SqlCommand cmd = new SqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@RacunId", racunId);
                    await cmd.ExecuteNonQueryAsync();

                    _logger.LogInformation($"[STRIPE WEBHOOK] Racun {racunId} oznacen kao Placen.");
                }
            }

            var odgovor = req.CreateResponse(HttpStatusCode.OK);
            return odgovor;
        }
    }
}