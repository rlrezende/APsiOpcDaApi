using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IDetectModelRepository : IGenericRepository<DetectModel>
    {
        Task<List<DetectModel>> GetByStatusAsync(DetectModelStatus status);
        Task<List<DetectModel>> GetActiveAsync();
        Task<List<DetectModel>> GetDraftsAsync();
        Task<DetectModel?> GetWithDetailsAsync(Guid id);
    }
}
