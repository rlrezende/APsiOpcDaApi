namespace APsiOpcDaApi.Domain.Entities
{
    public class DetectModelTag : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid DetectModelId { get; set; }
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public double SeverityBaseline { get; set; }
        public double? ExpectedStdDev { get; set; }
        public string PvMvRelation { get; set; } = "none";
        public string Notes { get; set; } = string.Empty;

        public virtual DetectModel DetectModel { get; set; } = null!;
    }
}

