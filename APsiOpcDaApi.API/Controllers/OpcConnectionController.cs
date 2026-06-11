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

        public OpcConnectionController(IOpcDiscoveryService discoveryService, IOpcServerService serverService, IOpcGroupService groupService)
        {
            _discoveryService = discoveryService;
            _serverService = serverService;
            _groupService = groupService;
        }

        [HttpPost("connect/{serverId}")]
        public async Task<IActionResult> ConnectToServer(Guid serverId)
        {
            try
            {
                var status = await _discoveryService.GetConnectionStatusAsync(serverId);
                
                if (status.IsConnected)
                {
                    return Ok(new { 
                        message = "Conectado com sucesso", 
                        serverId, 
                        serverName = status.ServerName,
                        endpoint = status.Endpoint,
                        connectedAt = DateTime.UtcNow 
                    });
                }
                else
                {
                    return BadRequest(new { 
                        message = "Falha na conexão", 
                        serverId,
                        error = status.ErrorMessage 
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
            var status = await _discoveryService.GetConnectionStatusAsync(serverId);
            return Ok(status);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveConnections()
        {
            var connections = await _discoveryService.GetActiveConnectionsAsync();
            return Ok(new { connections, totalActive = connections.Count });
        }
    }
}

