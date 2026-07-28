using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcGroupService : IGenericService<OpcGroup, OpcGroupDTO>
    {
        Task<List<OpcGroupDTO>> GetGroupsByServerIdAsync(Guid serverId);
        Task<List<OpcGroupDTO>> GetGroupsByUnidadeIdAsync(Guid unidadeId, bool activeOnly = false);
        Task<List<OpcGroupDTO>> GetActiveGroupsAsync();
        Task<OpcGroupDTO> GetGroupWithTagsAsync(Guid groupId);
        Task<bool> ActivateGroupAsync(Guid groupId);
        Task<bool> DeactivateGroupAsync(Guid groupId);
        Task<int> DeactivateGroupsByServerAsync(Guid serverId);
        Task<List<TagDTO>> GetGroupTagsAsync(Guid groupId);
        Task<bool> AddTagsToGroupAsync(Guid groupId, List<TagDTO> tags);
        Task<bool> RemoveTagFromGroupAsync(Guid groupId, Guid tagId);
    }
}

