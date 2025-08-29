using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opc-discovery")]
    public class OpcDiscoveryController : ControllerBase
    {
        private readonly IOpcDiscoveryService _discoveryService;

        public OpcDiscoveryController(IOpcDiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;
        }

        [HttpGet("scan")]
        public async Task<IActionResult> ScanNetwork([FromQuery] string? networkRange = null, [FromQuery] int timeout = 30)
        {
            try
            {
                var result = await _discoveryService.ScanNetworkAsync(networkRange, timeout);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro durante o escaneamento", error = ex.Message });
            }
        }

        [HttpPost("add-manual")]
        public async Task<IActionResult> AddManualServer([FromBody] AddManualServerRequest request)
        {
            try
            {
                var result = await _discoveryService.AddManualServerAsync(
                    request.Name, 
                    request.Endpoint, 
                    request.Username, 
                    request.Password);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Erro ao adicionar servidor", error = ex.Message });
            }
        }

        [HttpGet("servers")]
        public async Task<IActionResult> GetDiscoveredServers(
            [FromQuery] bool onlineOnly = false,
            [FromQuery] string? filter = null)
        {
            var servers = await _discoveryService.GetDiscoveredServersAsync(onlineOnly);
            
            if (!string.IsNullOrEmpty(filter))
            {
                servers = servers.Where(s => 
                    s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    s.Endpoint.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(new { servers, totalFound = servers.Count });
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            var isConnected = await _discoveryService.TestServerConnectionAsync(request.Endpoint);
            return Ok(new { endpoint = request.Endpoint, isConnected, testedAt = DateTime.UtcNow });
        }

        [HttpPost("discover-localhost")]
        public async Task<IActionResult> DiscoverAndSaveLocalhost([FromQuery] int port = 4840)
        {
            try
            {
                var endpoint = $"opc.tcp://localhost:{port}";
                
                // Testar se o servidor está online
                var isOnline = await _discoveryService.TestServerConnectionAsync(endpoint);
                
                if (!isOnline)
                {
                    return NotFound(new { 
                        message = "Servidor OPC não encontrado no localhost", 
                        endpoint = endpoint,
                        isOnline = false 
                    });
                }

                // Verificar se já existe
                var existingServers = await _discoveryService.GetDiscoveredServersAsync();
                var existingServer = existingServers.FirstOrDefault(s => s.Endpoint == endpoint);
                
                if (existingServer != null)
                {
                    return Ok(new { 
                        message = "Servidor localhost já existe", 
                        serverId = existingServer.Id,
                        endpoint = endpoint,
                        isOnline = true,
                        alreadyExists = true
                    });
                }

                // Adicionar novo servidor localhost
                var serverName = $"Localhost OPC Server (:{port})";
                var discoveredServer = await _discoveryService.AddManualServerAsync(serverName, endpoint);
                
                return Ok(new { 
                    message = "Servidor localhost descoberto e salvo com sucesso", 
                    serverId = discoveredServer.Id,
                    endpoint = endpoint,
                    name = serverName,
                    isOnline = true,
                    alreadyExists = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao descobrir servidor localhost", 
                    error = ex.Message 
                });
            }
        }
    }

    public class AddManualServerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string SecurityMode { get; set; } = "None";
    }

    public class TestConnectionRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
