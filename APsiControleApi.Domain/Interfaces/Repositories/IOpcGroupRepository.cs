using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IOpcGroupRepository : IGenericRepository<OpcGroup>
    {
        Task<List<OpcGroup>> GetGroupsByServerIdAsync(Guid serverId);
        Task<List<OpcGroup>> GetActiveGroupsAsync();
        Task<OpcGroup> GetGroupWithTagsAsync(Guid groupId);
    }
}
