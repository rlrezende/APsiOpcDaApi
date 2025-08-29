using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class OpcGroupRepository : GenericRepository<OpcGroup>, IOpcGroupRepository
    {
        public OpcGroupRepository(APsiControleApiContext context) : base(context)
        {
        }

        public async Task<List<OpcGroup>> GetGroupsByServerIdAsync(Guid serverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(g => g.ServerId == serverId)
                .Include(g => g.Server)
                .Include(g => g.Tags)
                .ToListAsync();
        }

        public async Task<List<OpcGroup>> GetActiveGroupsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(g => g.IsActive)
                .Include(g => g.Server)
                .Include(g => g.Tags)
                .ToListAsync();
        }

        public async Task<OpcGroup> GetGroupWithTagsAsync(Guid groupId)
        {
            return await _dbSet
                .Include(g => g.Server)
                .Include(g => g.Tags)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }
    }
}
