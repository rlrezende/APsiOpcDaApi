using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IOpcDiscoveredServerRepository : IGenericRepository<OpcDiscoveredServer>
    {
        Task<List<OpcDiscoveredServer>> GetOnlineServersAsync();
        Task<OpcDiscoveredServer> GetByEndpointAsync(string endpoint);
        Task<List<OpcDiscoveredServer>> GetByNetworkRangeAsync(string networkRange);
    }
}
