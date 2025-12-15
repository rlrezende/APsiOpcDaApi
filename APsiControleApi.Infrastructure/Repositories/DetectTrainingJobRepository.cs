using System;
using System.Linq;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class DetectTrainingJobRepository : GenericRepository<DetectTrainingJob>, IDetectTrainingJobRepository
    {
        public DetectTrainingJobRepository(APsiControleApiContext context) : base(context)
        {
        }

        public async Task<List<DetectTrainingJob>> GetRecentByModelAsync(Guid modelId, int take = 5)
        {
            return await _context.DetectTrainingJobs
                .AsNoTracking()
                .Where(job => job.DetectModelId == modelId)
                .OrderByDescending(job => job.CreatedDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(Guid jobId, DetectTrainingStatus status, string? notes = null)
        {
            var job = await _context.DetectTrainingJobs.FindAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Training job {jobId} not found", nameof(jobId));
            }

            job.Status = status;
            job.UpdatedDate = DateTime.UtcNow;

            if (status == DetectTrainingStatus.Completed)
            {
                job.CompletedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                job.Notes = notes;
            }

            await _context.SaveChangesAsync();
        }
    }
}
