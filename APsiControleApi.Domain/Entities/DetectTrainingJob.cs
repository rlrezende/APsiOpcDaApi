using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.Entities
{
    public class DetectTrainingJob : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid? DetectModelId { get; set; }
        public DetectTrainingStatus Status { get; set; } = DetectTrainingStatus.Pending;
        public DateTime? CompletedAt { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual DetectModel? DetectModel { get; set; }
    }
}
