using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class OpcBrowserController : ControllerBase
    {
        private readonly IOpcBrowserService _opcBrowserService;
        private readonly IOpcServerService _opcServerService;

        public OpcBrowserController(IOpcBrowserService opcBrowserService, IOpcServerService opcServerService)
        {
            _opcBrowserService = opcBrowserService;
            _opcServerService = opcServerService;
        }

        [HttpGet("nodes/{serverId}")]
        [AllowAnonymous]
        public async Task<IActionResult> BrowseNodes(Guid serverId, [FromQuery] string? parentNodeId = null, [FromQuery] Guid? unidadeId = null)
        {
            try
            {
                if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
                {
                    var servers = await _opcServerService.GetAllAsync();
                    var server = servers.FirstOrDefault(s => s.Id == serverId && s.ModuloId == unidadeId.Value);
                    if (server == null)
                    {
                        return NotFound(new { message = "Servidor OPC não pertence à unidade selecionada.", serverId, unidadeId });
                    }
                }

                var nodes = await _opcBrowserService.BrowseNodesAsync(serverId, parentNodeId);
                return Ok(nodes);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new
                {
                    message = ex.Message,
                    serverId
                });
            }
        }
    }
}
