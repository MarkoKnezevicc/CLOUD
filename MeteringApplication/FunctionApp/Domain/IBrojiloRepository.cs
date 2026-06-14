using System.Threading;
using System.Threading.Tasks;

namespace SmartMetering.AzureFunctions.Domain
{
    // DTO
    public class BrojiloInfo
    {
        public int Id { get; set; }
        public TipPrikljucka Tip { get; set; }
    }

    public interface IBrojiloRepository
    {
        Task<BrojiloInfo?> GetByDeviceTokenAsync(string deviceToken, CancellationToken ct = default);
    }
}