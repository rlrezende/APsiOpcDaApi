using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Domain.Interfaces.Repositories
{
    public interface IDetectModelRepository : IGenericRepository<DetectModel>
    {
        Task<List<DetectModel>> GetByStatusAsync(DetectModelStatus status);
        Task<List<DetectModel>> GetActiveAsync();
        Task<List<DetectModel>> GetDraftsAsync();
        Task<DetectModel?> GetWithDetailsAsync(Guid id);
    }
}

