using System.Linq;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class DetectModelRepository : GenericRepository<DetectModel>, IDetectModelRepository
    {
        public DetectModelRepository(APsiOpcDaApiContext context) : base(context)
        {
        }

        private IQueryable<DetectModel> BaseQuery(bool asNoTracking = true)
        {
            var query = _context.DetectModels
                .Include(model => model.Tags)
                .Include(model => model.Pipelines)
                .Include(model => model.TrainingJobs);

            return asNoTracking ? query.AsNoTracking() : query;
        }

        public async Task<List<DetectModel>> GetByStatusAsync(DetectModelStatus status)
        {
            return await BaseQuery()
                .Where(model => model.Status == status)
                .OrderByDescending(model => model.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DetectModel>> GetActiveAsync()
        {
            return await BaseQuery()
                .Where(model => model.Status == DetectModelStatus.Active || model.Status == DetectModelStatus.Paused)
                .OrderByDescending(model => model.DeployedAt)
                .ToListAsync();
        }

        public async Task<List<DetectModel>> GetDraftsAsync()
        {
            return await BaseQuery()
                .Where(model => model.Status == DetectModelStatus.Draft)
                .OrderByDescending(model => model.CreatedDate)
                .ToListAsync();
        }

        public async Task<DetectModel?> GetWithDetailsAsync(Guid id)
        {
            return await BaseQuery(asNoTracking: false)
                .FirstOrDefaultAsync(model => model.Id == id);
        }
    }
}

