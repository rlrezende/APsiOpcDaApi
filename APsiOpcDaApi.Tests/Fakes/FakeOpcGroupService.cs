using System.Linq.Expressions;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;

namespace APsiOpcDaApi.Tests.Fakes;

internal sealed class FakeOpcGroupService : IOpcGroupService
{
    public IReadOnlyList<OpcGroupDTO> AllGroups { get; set; } = [];
    public IReadOnlyList<OpcGroupDTO> GroupsByServer { get; set; } = [];
    public IReadOnlyList<OpcGroupDTO> GroupsByUnidade { get; set; } = [];
    public IReadOnlyList<OpcGroupDTO> ActiveGroups { get; set; } = [];
    public OpcGroupDTO? GroupWithTags { get; set; }

    public int GetAllCalls { get; private set; }
    public List<Guid> ServerRequests { get; } = [];
    public List<(Guid UnidadeId, bool ActiveOnly)> UnidadeRequests { get; } = [];
    public List<Guid> GroupWithTagsRequests { get; } = [];
    public int GetActiveCalls { get; private set; }

    public Task<IEnumerable<OpcGroupDTO>> GetAllAsync()
    {
        GetAllCalls++;
        return Task.FromResult<IEnumerable<OpcGroupDTO>>(AllGroups);
    }

    public Task<List<OpcGroupDTO>> GetGroupsByServerIdAsync(Guid serverId)
    {
        ServerRequests.Add(serverId);
        return Task.FromResult(GroupsByServer.ToList());
    }

    public Task<List<OpcGroupDTO>> GetGroupsByUnidadeIdAsync(Guid unidadeId, bool activeOnly = false)
    {
        UnidadeRequests.Add((unidadeId, activeOnly));
        return Task.FromResult(GroupsByUnidade.ToList());
    }

    public Task<List<OpcGroupDTO>> GetActiveGroupsAsync()
    {
        GetActiveCalls++;
        return Task.FromResult(ActiveGroups.ToList());
    }

    public Task<OpcGroupDTO> GetGroupWithTagsAsync(Guid groupId)
    {
        GroupWithTagsRequests.Add(groupId);
        return Task.FromResult(GroupWithTags!);
    }

    public Task<OpcGroupDTO> GetByIdAsync(Guid id) => throw NotConfigured();
    public Task<OpcGroupDTO> AddAsync(OpcGroupDTO dto) => throw NotConfigured();
    public Task AddRangeAsync(IEnumerable<OpcGroupDTO> dtos) => throw NotConfigured();
    public Task UpdateAsync(OpcGroupDTO dto) => throw NotConfigured();
    public Task DeleteAsync(Guid id) => throw NotConfigured();
    public Task<(IEnumerable<OpcGroupDTO> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize) =>
        throw NotConfigured();
    public Task<OpcGroup> GetByConditionAsync(Expression<Func<OpcGroup, bool>> condition) => throw NotConfigured();
    public Task<bool> ActivateGroupAsync(Guid groupId) => throw NotConfigured();
    public Task<bool> DeactivateGroupAsync(Guid groupId) => throw NotConfigured();
    public Task<int> DeactivateGroupsByServerAsync(Guid serverId) => throw NotConfigured();
    public Task<List<TagDTO>> GetGroupTagsAsync(Guid groupId) => throw NotConfigured();
    public Task<bool> AddTagsToGroupAsync(Guid groupId, List<TagDTO> tags) => throw NotConfigured();
    public Task<bool> RemoveTagFromGroupAsync(Guid groupId, Guid tagId) => throw NotConfigured();

    private static InvalidOperationException NotConfigured() =>
        new("Operação fora da fatia de consulta de grupos OPC DA.");
}
