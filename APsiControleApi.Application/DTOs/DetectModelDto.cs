using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class DetectModelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstrumentClass { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ScheduleMinutes { get; set; }
        public double TargetAccuracy { get; set; }
        public double DriftPercent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeployedAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public IEnumerable<string> Pipelines { get; set; } = Array.Empty<string>();
        public IEnumerable<DetectModelTagConfigDto> Tags { get; set; } = Array.Empty<DetectModelTagConfigDto>();
    }
}
