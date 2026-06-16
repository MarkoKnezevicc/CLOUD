using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace SmartMetering.AzureFunctions
{
    public static class SignalrHub
    {

        //funkcija koja prima klijentov zahtev za SignalR vezu i vraca AccessToken
        [Function("negotiate")]
        public static async Task<HttpResponseData> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "telemetrijaHub")] string connectionInfo)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(connectionInfo);
            return response;
        }

        //Funkcija koja ubacuje klijenta u grupu id-a serijskog broja, radi slanja telemetrije
        [Function("JoinGroup")]
        [SignalROutput(HubName = "telemetrijaHub")]
        public static async Task<SignalRGroupAction> JoinGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "joinGroup")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<JoinGroupDto>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.ConnectionId) || string.IsNullOrEmpty(data.GroupName))
            {
                return null!;
            }

            return new SignalRGroupAction(SignalRGroupActionType.Add)
            {
                ConnectionId = data.ConnectionId,
                GroupName = data.GroupName
            };
        }
    }

    public class JoinGroupDto
    {
        [JsonProperty("connectionId")] //Osigurava mapiranje sa frontenda
        public string ConnectionId { get; set; } = string.Empty;

        [JsonProperty("groupName")]    //Osigurava mapiranje sa frontenda
        public string GroupName { get; set; } = string.Empty;
    }
}