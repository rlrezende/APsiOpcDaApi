using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class OpcGroupRepository : GenericRepository<OpcGroup>, IOpcGroupRepository
    {
        public OpcGroupRepository(APsiOpcDaApiContext context) : base(context)
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

        public async Task<List<OpcGroup>> GetAllWithTagsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(group => group.Tags)
                .ToListAsync();
        }
    }
}

