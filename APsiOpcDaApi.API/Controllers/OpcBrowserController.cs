using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class OpcBrowserController : ControllerBase
    {
        private readonly IOpcBrowserService _opcBrowserService;

        public OpcBrowserController(IOpcBrowserService opcBrowserService)
        {
            _opcBrowserService = opcBrowserService;
        }

        [HttpGet("nodes/{serverId}")]
        [AllowAnonymous]
        public async Task<IActionResult> BrowseNodes(Guid serverId, [FromQuery] string? parentNodeId = null)
        {
            try
            {
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

