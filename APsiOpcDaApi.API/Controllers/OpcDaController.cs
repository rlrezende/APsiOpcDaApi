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
        private readonly IOpcDaClientService _opcDaClientService;

        public OpcDaController(
            IOpcServerService opcServerService,
            IOpcBrowserService opcBrowserService,
            IOpcDaClientService opcDaClientService)
        {
            _opcServerService = opcServerService;
            _opcBrowserService = opcBrowserService;
            _opcDaClientService = opcDaClientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? unidadeId = null)
        {
            if (!HasUnidade(unidadeId))
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            var servers = await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da);
            servers = servers.Where(s => s.ModuloId == unidadeId!.Value);
            return Ok(servers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            if (!HasUnidade(unidadeId))
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            var server = await GetDaServerInUnidadeAsync(id, unidadeId!.Value);
            if (server == null)
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
            if (!HasUnidade(unidadeId))
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            dto.ModuloId = unidadeId!.Value;
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
            if (!HasUnidade(unidadeId))
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            var existing = await GetDaServerInUnidadeAsync(id, unidadeId!.Value);
            if (existing == null)
            {
                return NotFound();
            }

            dto.Id = id;
            dto.ModuloId = unidadeId.Value;
            dto.Tipo = TipoOpcServer.Da;
            dto.Endpoint ??= dto.Host ?? string.Empty;

            await _opcServerService.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? unidadeId = null)
        {
            if (!HasUnidade(unidadeId))
            {
                return BadRequest(new { message = "UnidadeId é obrigatório." });
            }

            var existing = await GetDaServerInUnidadeAsync(id, unidadeId!.Value);
            if (existing == null)
            {
                return NotFound();
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

        [HttpPost("{id:guid}/write")]
        public async Task<IActionResult> WriteValue(Guid id, [FromBody] OpcDaWriteRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.ItemId))
                return BadRequest(new { message = "ItemId é obrigatório." });

            var server = await _opcServerService.GetByIdAsync(id);
            if (server == null || server.Tipo != TipoOpcServer.Da)
                return NotFound(new { message = "Servidor OPC DA não encontrado." });

            if (!_opcDaClientService.IsSupported)
                return BadRequest(new { message = "Write OPC DA só é suportado em ambiente Windows." });

            try
            {
                var ok = await _opcDaClientService.WriteValueAsync(server, dto.ItemId, dto.Value);
                return ok
                    ? Ok(new { success = true, itemId = dto.ItemId, value = dto.Value })
                    : StatusCode(500, new { message = "Write falhou no servidor OPC DA.", itemId = dto.ItemId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao escrever no servidor OPC DA.", error = ex.Message });
            }
        }

        [HttpGet("{id:guid}/browse")]
        public async Task<IActionResult> Browse(Guid id, [FromQuery] string? itemId = null, [FromQuery] Guid? unidadeId = null)
        {
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var server = await GetDaServerInUnidadeAsync(id, unidadeId.Value);
                if (server == null)
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

        private async Task<OpcServerDTO?> GetDaServerInUnidadeAsync(Guid serverId, Guid unidadeId)
        {
            var servers = await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da);
            return servers.FirstOrDefault(s => s.Id == serverId && s.ModuloId == unidadeId);
        }

        private static bool HasUnidade(Guid? unidadeId) =>
            unidadeId.HasValue && unidadeId.Value != Guid.Empty;

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
