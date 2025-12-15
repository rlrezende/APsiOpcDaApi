using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class DetectGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalTags { get; set; }
        public bool IsActive { get; set; }
        public string Cadence { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    }
}
