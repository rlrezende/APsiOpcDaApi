using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcDiscoveryService
    {
        Task<OpcDiscoveryResultDTO> ScanNetworkAsync(Guid unidadeId, string? networkRange = null, int timeout = 30);
        Task<OpcDiscoveredServerDTO> AddManualServerAsync(Guid unidadeId, string name, string endpoint, string? username = null, string? password = null);
        Task<List<OpcDiscoveredServerDTO>> GetDiscoveredServersAsync(bool onlineOnly = false);
        Task<bool> TestServerConnectionAsync(string endpoint);
        Task<OpcConnectionStatusDTO> GetConnectionStatusAsync(Guid serverId);
        Task<List<OpcConnectionStatusDTO>> GetActiveConnectionsAsync();
        Task<OpcDiscoveredServerDTO> DiscoverLocalhostAsync(Guid unidadeId, int port = 4840);
    }
}
