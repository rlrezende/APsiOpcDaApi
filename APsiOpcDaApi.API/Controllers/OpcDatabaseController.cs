using Microsoft.AspNetCore.Mvc;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.DTOs;
using System.Linq;

namespace APsiOpcDaApi.Controllers
{
   [ApiController]
[Route("api/[controller]")]
public class OpcDatabaseController : ControllerBase
{
    private readonly IDatabaseBrowserService _browserService;
    private readonly IOpcServerService _opcServerService;

    public OpcDatabaseController(IDatabaseBrowserService browserService, IOpcServerService opcServerService)
    {
        _browserService = browserService;
        _opcServerService = opcServerService;
    }

    [HttpGet("{serverId}/browse")]
    public async Task<ActionResult<OpcBrowseResultDTO>> BrowseNodes(Guid serverId, [FromQuery] string? parentNodeId = null, [FromQuery] Guid? unidadeId = null)
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

            var result = await _browserService.BrowseNodesAsync(serverId, parentNodeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}

}
