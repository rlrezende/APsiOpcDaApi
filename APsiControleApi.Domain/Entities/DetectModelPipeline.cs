namespace APsiControleApi.Domain.Entities
{
    public class DetectModelPipeline : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid DetectModelId { get; set; }
        public string PipelineKey { get; set; } = string.Empty;

        public virtual DetectModel DetectModel { get; set; } = null!;
    }
}
