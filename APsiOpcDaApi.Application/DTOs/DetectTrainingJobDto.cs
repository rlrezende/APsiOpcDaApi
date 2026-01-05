using System;

namespace APsiOpcDaApi.Application.DTOs
{
    public class DetectTrainingJobDto
    {
        public Guid Id { get; set; }
        public Guid? DetectModelId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}

