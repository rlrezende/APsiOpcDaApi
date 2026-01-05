using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace APsiOpcDaApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IGenericService<Tag, TagDTO> _genericService;

    public TagController(IGenericService<Tag, TagDTO> genericService, ITagService tagService)
    {
        _genericService = genericService;
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDTO>>> GetAll()
    {
        var tags = await _genericService.GetAllAsync();
        return Ok(tags);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDTO>> GetById(Guid id)
    {
        var tag = await _genericService.GetByIdAsync(id);
        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDTO>> Create([FromBody] TagDTO dto)
    {
        var created = await _genericService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TagDTO dto)
    {
        dto.Id = id;
        await _genericService.UpdateAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _genericService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("paginadas-com-leituras")]
    public async Task<IActionResult> GetPagedTagsWithReadings([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        if (pageIndex < 1 || pageSize <= 0)
        {
            return BadRequest("Os parâmetros de paginação são inválidos.");
        }

        var (itemsDto, totalItems) = await _tagService.GetPagedTagsWithReadingsAsync(pageIndex, pageSize);

        return Ok(new { totalItems, itemsDto });
    }
}

