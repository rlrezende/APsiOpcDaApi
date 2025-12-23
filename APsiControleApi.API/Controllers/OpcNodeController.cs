using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpcNodeController : ControllerBase
{
    private readonly IOpcNodeService _nodeService;

    public OpcNodeController(IOpcNodeService nodeService)
    {
        _nodeService = nodeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpcNodeDTO>>> GetAll()
    {
        var nodes = await _nodeService.GetAllAsync();
        return Ok(nodes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OpcNodeDTO>> GetById(Guid id)
    {
        var node = await _nodeService.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    [HttpPost]
    public async Task<ActionResult<OpcNodeDTO>> Create([FromBody] OpcNodeDTO dto)
    {
        var created = await _nodeService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OpcNodeDTO dto)
    {
        dto.Id = id;
        await _nodeService.UpdateAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _nodeService.DeleteAsync(id);
        return NoContent();
    }
}
