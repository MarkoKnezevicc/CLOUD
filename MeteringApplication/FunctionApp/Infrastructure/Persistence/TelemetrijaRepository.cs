using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using SmartMetering.AzureFunctions.Domain;

namespace SmartMetering.AzureFunctions.Infrastructure.Persistence
{
    public class TelemetrijaRepository : ITelemetrijaRepository
    {
        private readonly TableClient _tableClient;

        public TelemetrijaRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient("Telemetrije");
            _tableClient.CreateIfNotExists();
        }

        public async Task SaveAsync(Telemetrija telemetrija, CancellationToken ct = default)
        {
            var entity = TelemetrijaMapper.ToEntity(telemetrija);
            await _tableClient.AddEntityAsync(entity, ct);
        }
    }
}