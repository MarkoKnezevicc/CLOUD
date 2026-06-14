using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartMetering.AzureFunctions.Domain;

namespace SmartMetering.AzureFunctions.Infrastructure.Persistence
{
    public class BrojiloRepository : IBrojiloRepository
    {
        private readonly SmartMeteringDbContext _context;

        public BrojiloRepository(SmartMeteringDbContext context)
        {
            _context = context;
        }

        public async Task<BrojiloInfo?> GetByDeviceTokenAsync(string deviceToken, CancellationToken ct = default)
        {
            return await _context.PametnaBrojila
                .Where(b => b.DeviceAccessToken == deviceToken && b.Status == StatusUredjaja.Uparen)
                .Select(b => new BrojiloInfo
                {
                    Id = b.Id,
                    Tip = b.Tip
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}