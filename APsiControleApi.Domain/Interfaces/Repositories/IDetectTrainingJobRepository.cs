using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IDetectTrainingJobRepository : IGenericRepository<DetectTrainingJob>
    {
        Task<List<DetectTrainingJob>> GetRecentByModelAsync(Guid modelId, int take = 5);
        Task UpdateStatusAsync(Guid jobId, DetectTrainingStatus status, string? notes = null);
    }
}
