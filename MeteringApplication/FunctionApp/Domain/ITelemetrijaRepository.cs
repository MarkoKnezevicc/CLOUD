using System.Threading;
using System.Threading.Tasks;

namespace SmartMetering.AzureFunctions.Domain
{
    public interface ITelemetrijaRepository
    {
        Task SaveAsync(Telemetrija telemetrija, CancellationToken ct = default);
    }
}