using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Domain.Interfaces.Repositories
{
    public interface IDetectTrainingJobRepository : IGenericRepository<DetectTrainingJob>
    {
        Task<List<DetectTrainingJob>> GetRecentByModelAsync(Guid modelId, int take = 5);
        Task UpdateStatusAsync(Guid jobId, DetectTrainingStatus status, string? notes = null);
    }
}

