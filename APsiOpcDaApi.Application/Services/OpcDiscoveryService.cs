using APsiC.Operations;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
namespace APsiOpcDaApi.Application.Services;
// Legacy discovery URLs implemented UA probes inside the DA process. They cannot
// grant a protocol fallback. DA connections use configured resources and the
// admitted DA client; status/listing below read persisted metadata only.
public sealed class OpcDiscoveryService:IOpcDiscoveryService
{
    private readonly IOpcServerService servers;
    public OpcDiscoveryService(IOpcDiscoveredServerRepository repository,IOpcServerService opcServerService,IMapper mapper){servers=opcServerService;}
    private static OperationAdmissionDeniedException Unsupported()=>new("PROTOCOL_OWNER_MISMATCH");
    public Task<OpcDiscoveryResultDTO> ScanNetworkAsync(Guid unidadeId,string? networkRange=null,int timeout=30)=>throw Unsupported();
    public Task<OpcDiscoveredServerDTO> AddManualServerAsync(Guid unidadeId,string name,string endpoint,string? username=null,string? password=null)=>throw Unsupported();
    public Task<bool> TestServerConnectionAsync(string endpoint)=>throw Unsupported();
    public Task<OpcDiscoveredServerDTO> DiscoverLocalhostAsync(Guid unidadeId,int port=4840)=>throw Unsupported();
    public async Task<List<OpcDiscoveredServerDTO>> GetDiscoveredServersAsync(bool onlineOnly=false)
    {
        var owned=await servers.GetAllAsync();
        return owned.Where(s=>s.Tipo==TipoOpcServer.Da && (!onlineOnly || s.IsOnline)).Select(s=>new OpcDiscoveredServerDTO
        {
            Id=s.Id,Name=s.Nome,Endpoint=s.Endpoint,DiscoveryTime=s.DiscoveryTime??s.LastConnection??DateTime.MinValue,
            IsOnline=s.IsOnline,ResponseTime=s.ResponseTime,NetworkRange=s.Host??string.Empty
        }).ToList();
    }
    public async Task<OpcConnectionStatusDTO> GetConnectionStatusAsync(Guid serverId)
    {
        var server=await servers.GetByIdAsync(serverId);
        if(server is null)return new(){ServerId=serverId,IsConnected=false,Status="Server not found",ServerName=string.Empty,Endpoint=string.Empty};
        if(server.Tipo!=TipoOpcServer.Da)throw Unsupported();
        return new(){ServerId=server.Id,ServerName=server.Nome,Endpoint=server.Endpoint,IsConnected=server.IsConnected,
            LastConnection=server.LastConnection,Status=server.ConnectionStatus??(server.IsConnected?"Connected":"Disconnected"),ResponseTime=server.ResponseTime};
    }
    public async Task<List<OpcConnectionStatusDTO>> GetActiveConnectionsAsync()
    {
        var result=new List<OpcConnectionStatusDTO>();
        foreach(var server in (await servers.GetAllAsync()).Where(s=>s.Tipo==TipoOpcServer.Da && s.IsConnected))
            result.Add(await GetConnectionStatusAsync(server.Id));
        return result;
    }
}
