using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class DetectModelCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstrumentClass { get; set; } = string.Empty;
        public int ScheduleMinutes { get; set; }
        public double TargetAccuracy { get; set; }
        public bool DeployNow { get; set; }
        public IEnumerable<string> Pipelines { get; set; } = new List<string>();
        public IEnumerable<DetectModelTagConfigDto> Tags { get; set; } = new List<DetectModelTagConfigDto>();
    }
}

