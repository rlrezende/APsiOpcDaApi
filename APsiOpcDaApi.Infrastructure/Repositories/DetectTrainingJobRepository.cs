using System;
using System.Linq;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class DetectTrainingJobRepository : GenericRepository<DetectTrainingJob>, IDetectTrainingJobRepository
    {
        public DetectTrainingJobRepository(APsiOpcDaApiContext context) : base(context)
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

