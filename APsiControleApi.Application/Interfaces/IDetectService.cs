using APsiControleApi.Application.DTOs;
using System;

namespace APsiControleApi.Application.Interfaces
{
    public interface IDetectService
    {
        Task<IEnumerable<DetectGroupDto>> GetGroupsAsync();
        Task<IEnumerable<DetectTagDto>> SearchTagsAsync(string? searchTerm, string? instrumentClass, Guid? groupId, int? limit = null);
        Task<DetectModelsOverviewDto> GetModelsOverviewAsync();
        Task<DetectModelDto> CreateModelAsync(DetectModelCreateRequest request);
        Task DeployDraftAsync(Guid modelId);
        Task ToggleModelAsync(Guid modelId, bool isActive);
        Task<DetectTrainingJobDto> RequestRetrainAsync(Guid modelId);
        Task<IEnumerable<DetectTrainingJobDto>> GetRecentJobsAsync(Guid modelId, int take = 5);
        Task<DetectTagHistoryDto> GetTagHistoryAsync(Guid tagId, DateTime start, DateTime end);
    }
}
