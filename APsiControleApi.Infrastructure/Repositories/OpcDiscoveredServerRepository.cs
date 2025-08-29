using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class OpcDiscoveredServerRepository : GenericRepository<OpcDiscoveredServer>, IOpcDiscoveredServerRepository
    {
        public OpcDiscoveredServerRepository(APsiControleApiContext context) : base(context)
        {
        }

        public async Task<List<OpcDiscoveredServer>> GetOnlineServersAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.IsOnline)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<OpcDiscoveredServer> GetByEndpointAsync(string endpoint)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        }

        public async Task<List<OpcDiscoveredServer>> GetByNetworkRangeAsync(string networkRange)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.NetworkRange == networkRange)
                .OrderBy(s => s.DiscoveryTime)
                .ToListAsync();
        }
    }
}
