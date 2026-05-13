using System;
using System.Linq;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APsiOpcDaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpcServerController : ControllerBase
    {
        private readonly IOpcServerService _opcServerService;

        public OpcServerController(IOpcServerService opcServerService)
        {
            _opcServerService = opcServerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? unidadeId = null)
        {
            var servers = await _opcServerService.GetAllAsync();
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
                servers = servers.Where(s => s.ModuloId == unidadeId.Value);
            return Ok(servers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            var server = unidadeId.HasValue && unidadeId.Value != Guid.Empty
                ? await GetServerInUnidadeAsync(id, unidadeId.Value)
                : await _opcServerService.GetByIdAsync(id);
            if (server == null)
            {
                return NotFound();
            }

            return Ok(server);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OpcServerDTO serverDto, [FromQuery] Guid? unidadeId = null)
        {
            if (serverDto == null)
            {
                return BadRequest("Payload inválido.");
            }
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                serverDto.ModuloId = unidadeId.Value;
            }
            if (serverDto.ModuloId == Guid.Empty)
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            var created = await _opcServerService.AddAsync(serverDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OpcServerDTO serverDto, [FromQuery] Guid? unidadeId = null)
        {
            if (serverDto == null)
            {
                return BadRequest("Payload inválido.");
            }
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var existing = await GetServerInUnidadeAsync(id, unidadeId.Value);
                if (existing == null)
                {
                    return NotFound();
                }
                serverDto.ModuloId = unidadeId.Value;
            }

            serverDto.Id = id;
            await _opcServerService.UpdateAsync(serverDto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var existing = await GetServerInUnidadeAsync(id, unidadeId.Value);
                if (existing == null)
                {
                    return NotFound();
                }
            }
            await _opcServerService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            return Ok(new
            {
                opcDaSupported = _opcServerService.IsOpcDaSupported()
            });
        }

        private async Task<OpcServerDTO?> GetServerInUnidadeAsync(Guid serverId, Guid unidadeId)
        {
            var servers = await _opcServerService.GetAllAsync();
            return servers.FirstOrDefault(s => s.Id == serverId && s.ModuloId == unidadeId);
        }
    }
}
