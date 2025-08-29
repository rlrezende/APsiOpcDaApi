using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<(IEnumerable<Tag> items, int totalItems)> GetPagedTagsWithReadingsAsync(int pageIndex, int pageSize);
        Task<Guid?> GetOpcServerIdByTagIdAsync(Guid tagId);

        Task<IEnumerable<Tag>> GetTagsByServerAsync(Guid serverId, string origem);

    }
}
