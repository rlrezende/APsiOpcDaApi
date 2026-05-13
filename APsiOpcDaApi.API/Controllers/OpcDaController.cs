using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc;
using Opc.Da;
using OpcCom;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opcda")]
    public class OpcDaController : ControllerBase
    {
        private readonly IOpcServerService _opcServerService;
        private readonly IOpcBrowserService _opcBrowserService;

        public OpcDaController(IOpcServerService opcServerService, IOpcBrowserService opcBrowserService)
        {
            _opcServerService = opcServerService;
            _opcBrowserService = opcBrowserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? unidadeId = null)
        {
            var servers = await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da);
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                servers = servers.Where(s => s.ModuloId == unidadeId.Value);
            }
            return Ok(servers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            var server = await _opcServerService.GetByIdAsync(id);
            if (server == null || server.Tipo != TipoOpcServer.Da)
            {
                return NotFound();
            }
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty && server.ModuloId != unidadeId.Value)
            {
                return NotFound();
            }

            return Ok(server);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OpcServerDTO dto, [FromQuery] Guid? unidadeId = null)
        {
            if (dto == null)
            {
                return BadRequest("Payload inválido.");
            }
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                dto.ModuloId = unidadeId.Value;
            }
            if (dto.ModuloId == Guid.Empty)
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            dto.Tipo = TipoOpcServer.Da;
            dto.Endpoint ??= dto.Host ?? string.Empty;

            var created = await _opcServerService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OpcServerDTO dto, [FromQuery] Guid? unidadeId = null)
        {
            if (dto == null)
            {
                return BadRequest("Payload inválido.");
            }
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var existing = await _opcServerService.GetByIdAsync(id);
                if (existing == null || existing.ModuloId != unidadeId.Value)
                {
                    return NotFound();
                }
                dto.ModuloId = unidadeId.Value;
            }

            dto.Id = id;
            dto.Tipo = TipoOpcServer.Da;
            dto.Endpoint ??= dto.Host ?? string.Empty;

            await _opcServerService.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var existing = await _opcServerService.GetByIdAsync(id);
                if (existing == null || existing.ModuloId != unidadeId.Value)
                {
                    return NotFound();
                }
            }
            await _opcServerService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("discover-local")]
        public async Task<IActionResult> DiscoverLocal([FromQuery] Guid unidadeId, [FromQuery] string? host = null)
        {
            if (!_opcServerService.IsOpcDaSupported())
            {
                return BadRequest(new { message = "Descoberta OPC DA só é suportada em ambiente Windows." });
            }
            if (unidadeId == Guid.Empty)
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            try
            {
                var targetHost = string.IsNullOrWhiteSpace(host) ? "localhost" : host.Trim();

                var existing = (await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da))
                    .Where(s => s.ModuloId == unidadeId)
                    .GroupBy(s => $"{(s.Host ?? string.Empty).ToLowerInvariant()}|{(s.ProgId ?? s.Endpoint ?? string.Empty).ToLowerInvariant()}")
                    .ToDictionary(g => g.Key, g => g.First());

                using var enumerator = new ServerEnumerator();
                var servers = enumerator.GetAvailableServers(Specification.COM_DA_20, targetHost, null)
                    ?? Array.Empty<Opc.Server>();

                var discovered = new List<OpcServerDTO>();

                foreach (var opcServer in servers.OfType<Opc.Da.Server>())
                {
                    using var serverInstance = opcServer;

                    var url = serverInstance.Url;
                    var endpoint = url != null
                        ? $"{url.Scheme}://{url.HostName}/{url.Path}".TrimEnd('/')
                        : string.Empty;

                    var progId = ExtractProgId(url);
                    var clsId = ExtractClsId(url);
                    var keyHost = targetHost.ToLowerInvariant();
                    var keyProgId = (progId ?? endpoint ?? string.Empty).ToLowerInvariant();
                    var dictionaryKey = $"{keyHost}|{keyProgId}";

                    var dto = new OpcServerDTO
                    {
                        Nome = serverInstance.Name ?? progId ?? "Servidor OPC DA",
                        Endpoint = endpoint,
                        Host = targetHost,
                        ProgId = progId ?? endpoint,
                        ClsId = clsId,
                        Descricao = serverInstance.Name,
                        Tipo = TipoOpcServer.Da,
                        ModuloId = unidadeId,
                        DiscoveryTime = DateTime.UtcNow,
                        IsOnline = true
                    };

                    if (!existing.ContainsKey(dictionaryKey))
                    {
                        var created = await _opcServerService.AddAsync(dto);
                        existing[dictionaryKey] = created;
                    }

                    discovered.Add(existing[dictionaryKey]);
                }

                return Ok(new
                {
                    servers = discovered,
                    totalFound = discovered.Count,
                    host = targetHost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao descobrir servidores OPC DA.", error = ex.Message });
            }
        }

        [HttpGet("{id:guid}/browse")]
        public async Task<IActionResult> Browse(Guid id, [FromQuery] string? itemId = null, [FromQuery] Guid? unidadeId = null)
        {
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var server = await _opcServerService.GetByIdAsync(id);
                if (server == null || server.ModuloId != unidadeId.Value)
                {
                    return NotFound(new { message = "Servidor OPC não pertence à unidade selecionada.", serverId = id, unidadeId });
                }
            }

            var result = await _opcBrowserService.BrowseNodesAsync(id, itemId);
            return Ok(result);
        }

        private static string? ExtractProgId(URL? url)
        {
            if (url?.Path == null)
            {
                return null;
            }

            var segments = url.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return null;
            }

            var first = segments[0];
            if (first.StartsWith("{") && first.EndsWith("}", StringComparison.Ordinal))
            {
                return null;
            }

            return first;
        }

        private static string? ExtractClsId(URL? url)
        {
            if (url?.Path == null)
            {
                return null;
            }

            var segments = url.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.LastOrDefault();
            if (string.IsNullOrWhiteSpace(last))
            {
                return null;
            }

            if (last.StartsWith("{") && last.EndsWith("}", StringComparison.Ordinal))
            {
                return last.Trim('{', '}');
            }

            return null;
        }
    }
}
