using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#if SOFTING_OPC
using Softing.OPCToolbox;
using Softing.OPCToolbox.Client;
#else
using TitaniumAS.Opc.Client.Common;
#endif

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opcda")]
    public class OpcDaController : ControllerBase
    {
        private static readonly Guid DefaultUnidadeId = new Guid("7f9ab23c-9860-4daa-9489-e5806b9f63d1");

        private readonly IOpcServerService _opcServerService;
        private readonly IOpcBrowserService _opcBrowserService;

        public OpcDaController(IOpcServerService opcServerService, IOpcBrowserService opcBrowserService)
        {
            _opcServerService = opcServerService;
            _opcBrowserService = opcBrowserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var servers = await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da);
            return Ok(servers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var server = await _opcServerService.GetByIdAsync(id);
            if (server == null || server.Tipo != TipoOpcServer.Da)
            {
                return NotFound();
            }

            return Ok(server);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OpcServerDTO dto)
        {
            if (dto == null)
            {
                return BadRequest("Payload inválido.");
            }

            dto.Tipo = TipoOpcServer.Da;
            dto.Endpoint ??= dto.Host ?? string.Empty;

            var created = await _opcServerService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OpcServerDTO dto)
        {
            if (dto == null)
            {
                return BadRequest("Payload inválido.");
            }

            dto.Id = id;
            dto.Tipo = TipoOpcServer.Da;
            dto.Endpoint ??= dto.Host ?? string.Empty;

            await _opcServerService.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _opcServerService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("discover-local")]
        public async Task<IActionResult> DiscoverLocal([FromQuery] string? host = null)
        {
            if (!_opcServerService.IsOpcDaSupported())
            {
                return BadRequest(new { message = "Descoberta OPC DA só é suportada em ambiente Windows." });
            }

            try
            {
#if SOFTING_OPC
                var targetHost = string.IsNullOrWhiteSpace(host) ? "localhost" : host;

                var existing = (await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da))
                    .ToDictionary(
                        s => $"{(s.Host ?? string.Empty).ToLowerInvariant()}|{(s.ProgId ?? s.Endpoint ?? string.Empty).ToLowerInvariant()}",
                        s => s);

                var browser = new ServerBrowser(targetHost);
                var exec = new ExecutionOptions
                {
                    ExecutionType = EnumExecutionType.SYNCHRONOUS,
                    ExecutionContext = (uint)browser.GetHashCode()
                };

                ServerBrowserData[]? serverData;
                var browseResult = browser.Browse(
                    EnumOPCSpecification.DA20,
                    EnumServerBrowserData.SERVERBROWSERDATA_ALL,
                    out serverData,
                    exec);

                if (!ResultCode.SUCCEEDED(browseResult))
                {
                    return StatusCode(500, new { message = "Erro ao descobrir servidores OPC DA (Softing).", error = $"0x{browseResult:X8}" });
                }

                var discovered = new List<OpcServerDTO>();

                foreach (var desc in serverData ?? Array.Empty<ServerBrowserData>())
                {
                    var progId = desc.ProgId ?? desc.ProgIdVersionIndependent ?? desc.ClsId ?? desc.Url;
                    if (string.IsNullOrWhiteSpace(progId))
                    {
                        continue;
                    }

                    var hostKey = targetHost.ToLowerInvariant();
                    var key = $"{hostKey}|{progId.ToLowerInvariant()}";

                    var dto = new OpcServerDTO
                    {
                        Nome = desc.Description ?? progId,
                        Endpoint = desc.Url ?? progId,
                        Host = targetHost,
                        ProgId = desc.ProgId ?? progId,
                        ClsId = desc.ClsId,
                        Provider = desc.ProgIdVersionIndependent,
                        Descricao = desc.Description,
                        Tipo = TipoOpcServer.Da,
                        UnidadeId = DefaultUnidadeId,
                        DiscoveryTime = DateTime.UtcNow,
                        IsOnline = true
                    };

                    discovered.Add(dto);

                    if (!existing.ContainsKey(key))
                    {
                        var created = await _opcServerService.AddAsync(dto);
                        existing[key] = created;
                    }
                }

                return Ok(new
                {
                    servers = discovered,
                    totalFound = discovered.Count,
                    host = targetHost
                });
#else
                var enumerator = new OpcServerEnumeratorAuto();
                var targetHost = string.IsNullOrWhiteSpace(host) ? enumerator.Localhost : host;

                var existing = (await _opcServerService.GetServersByTypeAsync(TipoOpcServer.Da))
                    .ToDictionary(
                        s => $"{(s.Host ?? string.Empty).ToLowerInvariant()}|{(s.ProgId ?? s.Endpoint ?? string.Empty).ToLowerInvariant()}",
                        s => s);

                var discovered = new List<OpcServerDTO>();
                var descriptions = enumerator.Enumerate(targetHost, loadAllServerCategories: true, OpcServerCategory.OpcDaServers)
                                     ?? Array.Empty<OpcServerDescription>();

                foreach (var desc in descriptions)
                {
                    var progId = desc.ProgId ?? desc.VendorIndependentProgId ?? desc.UserType ?? desc.CLSID.ToString();
                    var hostKey = (desc.Host ?? targetHost).ToLowerInvariant();
                    var key = $"{hostKey}|{progId.ToLowerInvariant()}";

                    var dto = new OpcServerDTO
                    {
                        Nome = desc.UserType ?? progId,
                        Endpoint = progId,
                        Host = desc.Host ?? targetHost,
                        ProgId = progId,
                        ClsId = desc.CLSID.ToString(),
                        Provider = desc.VendorIndependentProgId,
                        Descricao = desc.UserType,
                        Tipo = TipoOpcServer.Da,
                        UnidadeId = DefaultUnidadeId,
                        DiscoveryTime = DateTime.UtcNow,
                        IsOnline = true
                    };

                    discovered.Add(dto);

                    if (!existing.ContainsKey(key))
                    {
                        var created = await _opcServerService.AddAsync(dto);
                        existing[key] = created;
                    }
                }

                return Ok(new
                {
                    servers = discovered,
                    totalFound = discovered.Count,
                    host = targetHost
                });
#endif
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao descobrir servidores OPC DA.", error = ex.Message });
            }
        }

        [HttpGet("{id:guid}/browse")]
        public async Task<IActionResult> Browse(Guid id, [FromQuery] string? itemId = null)
        {
            var result = await _opcBrowserService.BrowseNodesAsync(id, itemId);
            return Ok(result);
        }
    }
}
