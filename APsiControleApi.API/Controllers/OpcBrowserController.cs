using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APsiControleApi.API.Controllers
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
            var nodes = await _opcBrowserService.BrowseNodesAsync(serverId, parentNodeId);
            return Ok(nodes);
        }
    }
}
