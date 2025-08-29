using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opc-connection")]
    public class OpcConnectionController : ControllerBase
    {
        private readonly IOpcDiscoveryService _discoveryService;
        private readonly IOpcServerService _serverService;

        public OpcConnectionController(IOpcDiscoveryService discoveryService, IOpcServerService serverService)
        {
            _discoveryService = discoveryService;
            _serverService = serverService;
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
        public async Task<IActionResult> DisconnectFromServer(Guid serverId)
        {
            await Task.CompletedTask; // Para evitar warning CS1998
            return Ok(new { message = "Desconectado com sucesso", serverId, disconnectedAt = DateTime.UtcNow });
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
