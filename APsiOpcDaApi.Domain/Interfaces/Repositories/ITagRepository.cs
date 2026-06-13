using APsiOpcDaApi.Domain.Entities;

namespace APsiOpcDaApi.Domain.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<(IEnumerable<Tag> items, int totalItems)> GetPagedTagsWithReadingsAsync(int pageIndex, int pageSize);
        Task<Guid?> GetOpcServerIdByTagIdAsync(Guid tagId);

        Task<IEnumerable<Tag>> GetTagsByServerAsync(Guid serverId, string origem);
        Task<IEnumerable<Tag>> SearchTagsAsync(string? searchTerm, string? instrumentClass, Guid? groupId, int? limit = null);
        Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> ids);
        Task<Tag?> GetByNodeIdOpcAsync(string nodeIdOpc);
        Task AtualizarValoresAtuaisAsync(IReadOnlyDictionary<Guid, double> valores);
    }
}
