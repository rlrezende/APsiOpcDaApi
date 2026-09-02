using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opc-connection")]
    public class OpcConnectionController : ControllerBase
    {
        private readonly IOpcDiscoveryService _discoveryService;
        private readonly IOpcServerService _serverService;
        private readonly IOpcGroupService _groupService;
        private readonly IOpcDaClientService _opcDaClientService;

        public OpcConnectionController(IOpcDiscoveryService discoveryService, IOpcServerService serverService, IOpcGroupService groupService, IOpcDaClientService opcDaClientService)
        {
            _discoveryService = discoveryService;
            _serverService = serverService;
            _groupService = groupService;
            _opcDaClientService = opcDaClientService;
        }

        [HttpPost("connect/{serverId}")]
        public async Task<IActionResult> ConnectToServer(Guid serverId)
        {
            try
            {
                var server = await _serverService.GetByIdAsync(serverId);
                if (server == null) return NotFound(new { message = "Servidor OPC DA não encontrado.", serverId });

                server.IsActive = true;
                var connected = await _opcDaClientService.TestConnectionAsync(server);
                server.IsConnected = connected;
                server.IsOnline = connected;
                server.ConnectionStatus = connected ? "Connected" : "Disconnected";
                server.LastConnection = connected ? DateTime.UtcNow : server.LastConnection;
                server.ErrorMessage = connected ? null : "Falha na conexão com o servidor OPC DA.";
                await _serverService.UpdateAsync(server);
                
                if (connected)
                {
                    return Ok(new { 
                        message = "Conectado com sucesso", 
                        serverId, 
                        serverName = server.Nome,
                        endpoint = server.Endpoint,
                        connectedAt = DateTime.UtcNow 
                    });
                }
                else
                {
                    return BadRequest(new { 
                        message = "Falha na conexão", 
                        serverId,
                        error = server.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao conectar", error = ex.Message });
            }
        }

        [HttpPost("disconnect/{serverId}")]
        public async Task<IActionResult> DisconnectFromServer(Guid serverId, [FromQuery] Guid? unidadeId = null)
        {
            var server = await _serverService.GetByIdAsync(serverId);
            if (server == null)
            {
                return NotFound(new { message = "Servidor OPC nÃ£o encontrado.", serverId });
            }

            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty && server.ModuloId != unidadeId.Value)
            {
                return StatusCode(403, new { message = "Servidor OPC nÃ£o pertence Ã  unidade selecionada.", serverId, unidadeId });
            }

            server.IsConnected = false;
            server.IsOnline = false;
            server.IsActive = false;
            server.ConnectionStatus = "Disconnected";
            await _serverService.UpdateAsync(server);

            var pausedGroups = await _groupService.DeactivateGroupsByServerAsync(serverId);

            return Ok(new
            {
                message = "Desconectado com sucesso. Grupos do servidor pausados.",
                serverId,
                pausedGroups,
                disconnectedAt = DateTime.UtcNow
            });
        }

        [HttpGet("status/{serverId}")]
        public async Task<IActionResult> GetConnectionStatus(Guid serverId)
        {
            var server = await _serverService.GetByIdAsync(serverId);
            if (server == null) return NotFound(new { message = "Servidor OPC DA não encontrado.", serverId });

            var connected = await _opcDaClientService.TestConnectionAsync(server);
            server.IsConnected = connected;
            server.IsOnline = connected;
            server.ConnectionStatus = connected ? "Connected" : "Disconnected";
            server.LastConnection = connected ? DateTime.UtcNow : server.LastConnection;
            server.ErrorMessage = connected ? null : "Falha na conexão com o servidor OPC DA.";
            await _serverService.UpdateAsync(server);

            return Ok(new
            {
                serverId,
                serverName = server.Nome,
                endpoint = server.Endpoint,
                isConnected = connected,
                status = server.ConnectionStatus,
                lastConnection = server.LastConnection,
                errorMessage = server.ErrorMessage
            });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveConnections()
        {
            var connections = await _discoveryService.GetActiveConnectionsAsync();
            return Ok(new { connections, totalActive = connections.Count });
        }
    }
}

