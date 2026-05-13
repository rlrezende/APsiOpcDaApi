using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opc-groups")]
    public class OpcGroupController : ControllerBase
    {
        private readonly IOpcGroupService _groupService;
        private readonly IOpcServerService _serverService;

        public OpcGroupController(IOpcGroupService groupService, IOpcServerService serverService)
        {
            _groupService = groupService;
            _serverService = serverService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroups([FromQuery] Guid? unidadeId = null)
        {
            var groups = await _groupService.GetAllAsync();
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var serverIds = (await _serverService.GetAllAsync())
                    .Where(s => s.ModuloId == unidadeId.Value)
                    .Select(s => s.Id)
                    .ToHashSet();

                groups = groups.Where(g => serverIds.Contains(g.ServerId));
            }

            return Ok(new { groups });
        }

        [HttpGet("server/{serverId}")]
        public async Task<IActionResult> GetGroupsByServer(Guid serverId, [FromQuery] Guid? unidadeId = null)
        {
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var server = await GetServerInUnidadeAsync(serverId, unidadeId.Value);
                if (server == null)
                {
                    return Ok(Enumerable.Empty<OpcGroupDTO>());
                }
            }

            var groups = await _groupService.GetGroupsByServerIdAsync(serverId);
            return Ok(groups);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveGroups([FromQuery] Guid? unidadeId = null)
        {
            var groups = await _groupService.GetActiveGroupsAsync();
            if (unidadeId.HasValue && unidadeId.Value != Guid.Empty)
            {
                var serverIds = (await _serverService.GetAllAsync())
                    .Where(s => s.ModuloId == unidadeId.Value)
                    .Select(s => s.Id)
                    .ToHashSet();

                groups = groups.Where(g => serverIds.Contains(g.ServerId)).ToList();
            }

            return Ok(groups);
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroup(Guid groupId)
        {
            var group = await _groupService.GetGroupWithTagsAsync(groupId);
            if (group == null)
                return NotFound();

            return Ok(group);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] OpcGroupDTO groupDto, [FromQuery] Guid? unidadeId = null)
        {
            var validation = await ValidateGroupServerAsync(groupDto.ServerId, unidadeId);
            if (validation != null)
            {
                return validation;
            }

            var createdGroup = await _groupService.AddAsync(groupDto);
            return CreatedAtAction(nameof(GetGroup), new { groupId = createdGroup.Id }, createdGroup);
        }

        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] OpcGroupDTO groupDto, [FromQuery] Guid? unidadeId = null)
        {
            groupDto.Id = groupId;
            var validation = await ValidateGroupServerAsync(groupDto.ServerId, unidadeId);
            if (validation != null)
            {
                return validation;
            }

            await _groupService.UpdateAsync(groupDto);
            return NoContent();
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            await _groupService.DeleteAsync(groupId);
            return NoContent();
        }

        [HttpPost("{groupId}/activate")]
        public async Task<IActionResult> ActivateGroup(Guid groupId)
        {
            var result = await _groupService.ActivateGroupAsync(groupId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Grupo ativado com sucesso", isActive = true });
        }

        [HttpPost("{groupId}/deactivate")]
        public async Task<IActionResult> DeactivateGroup(Guid groupId)
        {
            var result = await _groupService.DeactivateGroupAsync(groupId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Grupo desativado com sucesso", isActive = false });
        }

        [HttpGet("{groupId}/tags")]
        public async Task<IActionResult> GetGroupTags(Guid groupId)
        {
            var tags = await _groupService.GetGroupTagsAsync(groupId);
            return Ok(tags);
        }

        [HttpPost("{groupId}/tags")]
        public async Task<IActionResult> AddTagsToGroup(Guid groupId, [FromBody] AddTagsWithNodesToGroupRequest request)
        {
            var result = await _groupService.AddTagsToGroupAsync(groupId, request.Tags);
            if (!result)
                return BadRequest("Erro ao adicionar tags ao grupo");

            return Ok(new { message = "Tags adicionadas com sucesso" });
        }


        [HttpDelete("{groupId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTagFromGroup(Guid groupId, Guid tagId)
        {
            var result = await _groupService.RemoveTagFromGroupAsync(groupId, tagId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Tag removida do grupo com sucesso" });
        }

        private async Task<OpcServerDTO?> GetServerInUnidadeAsync(Guid serverId, Guid unidadeId)
        {
            var servers = await _serverService.GetAllAsync();
            return servers.FirstOrDefault(s => s.Id == serverId && s.ModuloId == unidadeId);
        }

        private async Task<IActionResult?> ValidateGroupServerAsync(Guid serverId, Guid? unidadeId)
        {
            if (!unidadeId.HasValue || unidadeId.Value == Guid.Empty || serverId == Guid.Empty)
            {
                return null;
            }

            var server = await GetServerInUnidadeAsync(serverId, unidadeId.Value);
            if (server == null)
            {
                return StatusCode(403, new { message = "Servidor OPC não pertence à unidade selecionada ou o usuário não possui acesso a ela." });
            }

            return null;
        }
    }

        public class AddTagsWithNodesToGroupRequest
        {
            public List<OpcNodeDTO> Nodes { get; set; } = new();
            public List<TagDTO> Tags { get; set; } = new();
        }

}
