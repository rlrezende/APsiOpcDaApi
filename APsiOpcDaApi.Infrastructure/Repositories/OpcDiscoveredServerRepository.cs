using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class OpcDiscoveredServerRepository : GenericRepository<OpcDiscoveredServer>, IOpcDiscoveredServerRepository
    {
        public OpcDiscoveredServerRepository(APsiOpcDaApiContext context) : base(context)
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

