using System;
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
        public async Task<IActionResult> GetAll()
        {
            var servers = await _opcServerService.GetAllAsync();
            return Ok(servers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var server = await _opcServerService.GetByIdAsync(id);
            if (server == null)
            {
                return NotFound();
            }

            return Ok(server);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OpcServerDTO serverDto)
        {
            if (serverDto == null)
            {
                return BadRequest("Payload inválido.");
            }

            var created = await _opcServerService.AddAsync(serverDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OpcServerDTO serverDto)
        {
            if (serverDto == null)
            {
                return BadRequest("Payload inválido.");
            }

            serverDto.Id = id;
            await _opcServerService.UpdateAsync(serverDto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
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
    }
}

