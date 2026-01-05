using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class DetectModelsOverviewDto
    {
        public IEnumerable<DetectModelDto> Production { get; set; } = new List<DetectModelDto>();
        public IEnumerable<DetectModelDto> Drafts { get; set; } = new List<DetectModelDto>();
    }
}

