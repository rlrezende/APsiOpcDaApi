using System;
using System.Collections.Generic;
using System.Linq;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Application.Services
{
    public class DetectService : IDetectService
    {
        private readonly IOpcGroupRepository _opcGroupRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IDetectModelRepository _detectModelRepository;
        private readonly IDetectTrainingJobRepository _trainingJobRepository;
        private readonly ILeituraService _leituraService;

        public DetectService(
            IOpcGroupRepository opcGroupRepository,
            ITagRepository tagRepository,
            IDetectModelRepository detectModelRepository,
            IDetectTrainingJobRepository trainingJobRepository,
            ILeituraService leituraService)
        {
            _opcGroupRepository = opcGroupRepository;
            _tagRepository = tagRepository;
            _detectModelRepository = detectModelRepository;
            _trainingJobRepository = trainingJobRepository;
            _leituraService = leituraService;
        }

        public async Task<IEnumerable<DetectGroupDto>> GetGroupsAsync()
        {
            var groups = await _opcGroupRepository.GetAllWithTagsAsync();

            return groups.Select(group => new DetectGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                TotalTags = group.Tags?.Count ?? 0,
                IsActive = group.IsActive,
                Cadence = group.UpdateRate > 0 ? $"{group.UpdateRate} ms" : string.Empty,
                Tags = group.Tags?.Select(tag => tag.Nome) ?? Array.Empty<string>()
            });
        }

        public async Task<IEnumerable<DetectTagDto>> SearchTagsAsync(string? searchTerm, string? instrumentClass, Guid? groupId, int? limit = null)
        {
            var tags = await _tagRepository.SearchTagsAsync(searchTerm, instrumentClass, groupId, limit);

            return tags.Select(tag => new DetectTagDto
            {
                Id = tag.Id,
                Name = tag.Nome,
                Description = tag.Descricao,
                GroupId = tag.GroupId,
                GroupName = tag.Group?.Name ?? string.Empty,
                UnidadeId = tag.UnidadeId,
                InstrumentClass = ResolveInstrumentClass(tag.Nome),
                Isa = ResolveIsa(tag.Nome),
                Area = tag.Group?.Description ?? string.Empty
            });
        }

        public async Task<DetectModelsOverviewDto> GetModelsOverviewAsync()
        {
            var production = await _detectModelRepository.GetActiveAsync();
            var drafts = await _detectModelRepository.GetDraftsAsync();

            return new DetectModelsOverviewDto
            {
                Production = production.Select(ToDto),
                Drafts = drafts.Select(ToDto)
            };
        }

        public async Task<DetectModelDto> CreateModelAsync(DetectModelCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("O nome do modelo é obrigatório.");
            }

            if (request.ScheduleMinutes <= 0)
            {
                throw new ArgumentException("A agenda Tdetect deve ser maior que zero.");
            }

            if (request.Tags == null || !request.Tags.Any())
            {
                throw new ArgumentException("É necessário informar ao menos uma tag para o modelo.");
            }

            var tagConfigs = request.Tags.ToList();
            var tagIds = tagConfigs.Select(tag => tag.TagId).ToList();
            var tagEntities = await _tagRepository.GetByIdsAsync(tagIds);
            var tagLookup = tagEntities.ToDictionary(tag => tag.Id, tag => tag.Nome);

            var pipelines = request.Pipelines?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            var now = DateTime.UtcNow;
            var model = new DetectModel
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                InstrumentClass = request.InstrumentClass?.Trim() ?? string.Empty,
                ScheduleMinutes = request.ScheduleMinutes,
                TargetAccuracy = request.TargetAccuracy,
                Status = request.DeployNow ? DetectModelStatus.Active : DetectModelStatus.Draft,
                IsActive = request.DeployNow,
                CreatedDate = now,
                DeployedAt = request.DeployNow ? now : null,
                LastRunAt = request.DeployNow ? now : null,
            };

            model.Pipelines = pipelines
                .Select(pipeline => new DetectModelPipeline
                {
                    Id = Guid.NewGuid(),
                    DetectModelId = model.Id,
                    PipelineKey = pipeline!,
                    CreatedDate = now
                })
                .ToList();

            model.Tags = tagConfigs
                .Select(tagConfig => new DetectModelTag
                {
                    Id = Guid.NewGuid(),
                    DetectModelId = model.Id,
                    TagId = tagConfig.TagId,
                    TagName = tagLookup.TryGetValue(tagConfig.TagId, out var tagName) ? tagName : tagConfig.TagName,
                    SeverityBaseline = tagConfig.SeverityBaseline,
                    ExpectedStdDev = tagConfig.ExpectedStdDev,
                    PvMvRelation = tagConfig.PvMvRelation ?? "none",
                    Notes = tagConfig.Notes ?? string.Empty,
                    CreatedDate = now
                })
                .ToList();

            await _detectModelRepository.AddAsync(model);

            if (request.DeployNow)
            {
                var job = new DetectTrainingJob
                {
                    Id = Guid.NewGuid(),
                    DetectModelId = model.Id,
                    Status = DetectTrainingStatus.Completed,
                    CreatedDate = now,
                    CompletedAt = now,
                    Notes = "Modelo implantado juntamente com o treinamento inicial."
                };

                await _trainingJobRepository.AddAsync(job);
            }

            return ToDto(model);
        }

        public async Task DeployDraftAsync(Guid modelId)
        {
            var model = await _detectModelRepository.GetWithDetailsAsync(modelId)
                ?? throw new ArgumentException("Modelo não encontrado", nameof(modelId));

            model.Status = DetectModelStatus.Active;
            model.IsActive = true;
            model.DeployedAt = DateTime.UtcNow;
            model.LastRunAt = DateTime.UtcNow;
            model.UpdatedDate = DateTime.UtcNow;

            await _detectModelRepository.UpdateAsync(model);
        }

        public async Task ToggleModelAsync(Guid modelId, bool isActive)
        {
            var model = await _detectModelRepository.GetWithDetailsAsync(modelId)
                ?? throw new ArgumentException("Modelo não encontrado", nameof(modelId));

            model.IsActive = isActive;
            model.Status = isActive ? DetectModelStatus.Active : DetectModelStatus.Paused;
            model.UpdatedDate = DateTime.UtcNow;

            await _detectModelRepository.UpdateAsync(model);
        }

        public async Task<DetectTrainingJobDto> RequestRetrainAsync(Guid modelId)
        {
            var now = DateTime.UtcNow;
            var job = new DetectTrainingJob
            {
                Id = Guid.NewGuid(),
                DetectModelId = modelId,
                Status = DetectTrainingStatus.Running,
                CreatedDate = now,
                Notes = "Job de retreino iniciado."
            };

            await _trainingJobRepository.AddAsync(job);

            await _trainingJobRepository.UpdateStatusAsync(job.Id, DetectTrainingStatus.Completed, "Retreino concluído com sucesso.");

            job.Status = DetectTrainingStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedDate = DateTime.UtcNow;
            job.Notes = "Retreino concluído com sucesso.";

            return ToDto(job);
        }

        public async Task<IEnumerable<DetectTrainingJobDto>> GetRecentJobsAsync(Guid modelId, int take = 5)
        {
            var jobs = await _trainingJobRepository.GetRecentByModelAsync(modelId, take);
            return jobs.Select(ToDto);
        }

        public async Task<DetectTagHistoryDto> GetTagHistoryAsync(Guid tagId, DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("O período informado é inválido.");
            }

            var tag = await _tagRepository.GetByIdAsync(tagId)
                ?? throw new ArgumentException("Tag não encontrada", nameof(tagId));

            var startUtc = start.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(start, DateTimeKind.Utc) : start.ToUniversalTime();
            var endUtc = end.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : end.ToUniversalTime();

            var readings = await _leituraService.ObterLeiturasPorPeriodoETagsAsync(
                tag.UnidadeId,
                startUtc,
                endUtc,
                new List<Guid> { tagId });

            var orderedReadings = readings
                .Where(r => r.TagId == tagId)
                .OrderBy(r => r.DataLeitura)
                .ToList();

            double? min = null;
            double? max = null;
            double? avg = null;

            if (orderedReadings.Count > 0)
            {
                var values = orderedReadings.Select(r => r.Valor);
                min = values.Min();
                max = values.Max();
                avg = values.Average();
            }

            return new DetectTagHistoryDto
            {
                TagId = tag.Id,
                TagName = tag.Nome,
                UnidadeId = tag.UnidadeId,
                Start = startUtc,
                End = endUtc,
                Samples = orderedReadings.Count,
                Min = min,
                Max = max,
                Average = avg,
                Points = orderedReadings.Select(r => new DetectTagHistoryPointDto
                {
                    Timestamp = DateTime.SpecifyKind(r.DataLeitura, DateTimeKind.Utc),
                    Value = r.Valor,
                }).ToList(),
            };
        }

        private DetectModelDto ToDto(DetectModel model)
        {
            return new DetectModelDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                InstrumentClass = model.InstrumentClass,
                IsActive = model.IsActive,
                Status = model.Status.ToString(),
                ScheduleMinutes = model.ScheduleMinutes,
                TargetAccuracy = model.TargetAccuracy,
                DriftPercent = model.DriftPercent,
                CreatedAt = model.CreatedDate,
                DeployedAt = model.DeployedAt,
                LastRunAt = model.LastRunAt,
                Pipelines = model.Pipelines?.Select(p => p.PipelineKey) ?? Array.Empty<string>(),
                Tags = model.Tags?.Select(tag => new DetectModelTagConfigDto
                {
                    TagId = tag.TagId,
                    TagName = tag.TagName,
                    SeverityBaseline = tag.SeverityBaseline,
                    ExpectedStdDev = tag.ExpectedStdDev,
                    PvMvRelation = tag.PvMvRelation,
                    Notes = tag.Notes
                }) ?? Array.Empty<DetectModelTagConfigDto>()
            };
        }

        private DetectTrainingJobDto ToDto(DetectTrainingJob job)
        {
            return new DetectTrainingJobDto
            {
                Id = job.Id,
                DetectModelId = job.DetectModelId,
                Status = job.Status.ToString(),
                CreatedAt = job.CreatedDate,
                CompletedAt = job.CompletedAt,
                Notes = job.Notes
            };
        }

        private string ResolveInstrumentClass(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return "Desconhecido";
            }

            var suffix = ResolveIsa(tagName);
            if (suffix.Equals("MV", StringComparison.OrdinalIgnoreCase) ||
                suffix.Equals("CV", StringComparison.OrdinalIgnoreCase) ||
                suffix.Equals("FV", StringComparison.OrdinalIgnoreCase))
            {
                return "Atuadores";
            }

            return "Medidores";
        }

        private string ResolveIsa(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return string.Empty;
            }

            var index = tagName.LastIndexOf('.');
            var suffix = index >= 0 ? tagName[(index + 1)..] : tagName;
            return suffix.ToUpperInvariant();
        }
    }
}
