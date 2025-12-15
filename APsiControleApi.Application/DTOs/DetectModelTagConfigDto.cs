using System;

namespace APsiControleApi.Application.DTOs
{
    public class DetectModelTagConfigDto
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public double SeverityBaseline { get; set; }
        public double? ExpectedStdDev { get; set; }
        public string PvMvRelation { get; set; } = "none";
        public string Notes { get; set; } = string.Empty;
    }
}
