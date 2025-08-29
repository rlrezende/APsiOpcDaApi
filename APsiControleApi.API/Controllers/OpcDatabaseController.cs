using Microsoft.AspNetCore.Mvc;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Controllers
{
   [ApiController]
[Route("api/[controller]")]
public class OpcDatabaseController : ControllerBase
{
    private readonly IDatabaseBrowserService _browserService;

    public OpcDatabaseController(IDatabaseBrowserService browserService)
    {
        _browserService = browserService;
    }

    [HttpGet("{serverId}/browse")]
    public async Task<ActionResult<OpcBrowseResultDTO>> BrowseNodes(Guid serverId, [FromQuery] string? parentNodeId = null)
    {
        try
        {
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
