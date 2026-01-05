using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APsiOpcDaApi.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/opc-groups")]
    public class OpcGroupController : ControllerBase
    {
        private readonly IOpcGroupService _groupService;

        public OpcGroupController(IOpcGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var groups = await _groupService.GetAllAsync();
            return Ok(new { groups });
        }

        [HttpGet("server/{serverId}")]
        public async Task<IActionResult> GetGroupsByServer(Guid serverId)
        {
            var groups = await _groupService.GetGroupsByServerIdAsync(serverId);
            return Ok(groups);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveGroups()
        {
            var groups = await _groupService.GetActiveGroupsAsync();
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
        public async Task<IActionResult> CreateGroup([FromBody] OpcGroupDTO groupDto)
        {
            var createdGroup = await _groupService.AddAsync(groupDto);
            return CreatedAtAction(nameof(GetGroup), new { groupId = createdGroup.Id }, createdGroup);
        }

        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] OpcGroupDTO groupDto)
        {
            groupDto.Id = groupId;
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
    }

        public class AddTagsWithNodesToGroupRequest
        {
            public List<OpcNodeDTO> Nodes { get; set; } = new();
            public List<TagDTO> Tags { get; set; } = new();
        }

}

