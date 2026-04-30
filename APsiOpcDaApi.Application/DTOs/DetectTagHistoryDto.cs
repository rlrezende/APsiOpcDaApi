using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class DetectTagHistoryDto
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public Guid ModuloId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Samples { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Average { get; set; }
        public IEnumerable<DetectTagHistoryPointDto> Points { get; set; } = Array.Empty<DetectTagHistoryPointDto>();
    }
}

