using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcDiscoveryService
    {
        Task<OpcDiscoveryResultDTO> ScanNetworkAsync(string? networkRange = null, int timeout = 30);
        Task<OpcDiscoveredServerDTO> AddManualServerAsync(string name, string endpoint, string? username = null, string? password = null);
        Task<List<OpcDiscoveredServerDTO>> GetDiscoveredServersAsync(bool onlineOnly = false);
        Task<bool> TestServerConnectionAsync(string endpoint);
        Task<OpcConnectionStatusDTO> GetConnectionStatusAsync(Guid serverId);
        Task<List<OpcConnectionStatusDTO>> GetActiveConnectionsAsync();
        Task<OpcDiscoveredServerDTO> DiscoverLocalhostAsync(int port = 4840);
    }
}
