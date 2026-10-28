using APsiC.Licensing.Infrastructure;
using APsiOpcDaApi.API.Services;
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
        private readonly QuotaHttpMutation? _mutations;

        public OpcServerController(IOpcServerService opcServerService, QuotaHttpMutation? mutations = null)
        {
            _opcServerService = opcServerService;
            _mutations = mutations;
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

            if (_mutations is not null)
                return await _mutations.ExecuteAsync(this, serverDto, async () =>
                {
                    var saved = await _opcServerService.AddAsync(serverDto);
                    var acknowledgement=new{saved.Id,saved.Nome,saved.ModuloId,UnidadeId=saved.ModuloId,saved.Tipo};
                    var location = Url.Action(nameof(GetById), new { id = saved.Id, unidadeId = saved.ModuloId });
                    return new QuotaHttpResponse(201, QuotaHttpMutation.Serialize(acknowledgement), saved.ModuloId, location);
                });

            var created = await _opcServerService.AddAsync(serverDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OpcServerDTO serverDto, [FromQuery] Guid? unidadeId = null)
        {
            if (serverDto is null) return BadRequest("Payload inválido.");
            if (_mutations is not null)
                return await _mutations.ExecuteAsync(this, new { id, unidadeId, value = serverDto }, async () =>
                {
                    var existing = await _opcServerService.GetByIdAsync(id);
                    if (existing is null || (unidadeId.HasValue && unidadeId.Value != Guid.Empty && existing.ModuloId != unidadeId.Value)) return null;
                    serverDto.Id = id;
                    serverDto.ModuloId = unidadeId.HasValue && unidadeId.Value != Guid.Empty ? unidadeId.Value : serverDto.ModuloId != Guid.Empty ? serverDto.ModuloId : existing.ModuloId;
                    await _opcServerService.UpdateAsync(serverDto);
                    return new QuotaHttpResponse(204, null, serverDto.ModuloId);
                });

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
            if (_mutations is not null)
                return await _mutations.ExecuteAsync(this, new { id, unidadeId }, async () =>
                {
                    var existing = await _opcServerService.GetByIdAsync(id);
                    if (existing is null || (unidadeId.HasValue && unidadeId.Value != Guid.Empty && existing.ModuloId != unidadeId.Value)) return null;
                    await _opcServerService.DeleteAsync(id);
                    return new QuotaHttpResponse(204, null, existing.ModuloId);
                });

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
