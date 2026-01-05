using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Domain.Entities
{
    public class DetectModel : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstrumentClass { get; set; } = string.Empty;
        public int ScheduleMinutes { get; set; }
        public double TargetAccuracy { get; set; }
        public DetectModelStatus Status { get; set; } = DetectModelStatus.Draft;
        public bool IsActive { get; set; }
        public DateTime? DeployedAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public double DriftPercent { get; set; }

        public virtual ICollection<DetectModelTag> Tags { get; set; } = new List<DetectModelTag>();
        public virtual ICollection<DetectModelPipeline> Pipelines { get; set; } = new List<DetectModelPipeline>();
        public virtual ICollection<DetectTrainingJob> TrainingJobs { get; set; } = new List<DetectTrainingJob>();
    }
}

