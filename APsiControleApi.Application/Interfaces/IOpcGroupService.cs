using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcGroupService : IGenericService<OpcGroup, OpcGroupDTO>
    {
        Task<List<OpcGroupDTO>> GetGroupsByServerIdAsync(Guid serverId);
        Task<List<OpcGroupDTO>> GetActiveGroupsAsync();
        Task<OpcGroupDTO> GetGroupWithTagsAsync(Guid groupId);
        Task<bool> ActivateGroupAsync(Guid groupId);
        Task<bool> DeactivateGroupAsync(Guid groupId);
        Task<List<TagDTO>> GetGroupTagsAsync(Guid groupId);
        Task<bool> AddTagsToGroupAsync(Guid groupId, List<TagDTO> tags);
        Task<bool> RemoveTagFromGroupAsync(Guid groupId, Guid tagId);
    }
}
