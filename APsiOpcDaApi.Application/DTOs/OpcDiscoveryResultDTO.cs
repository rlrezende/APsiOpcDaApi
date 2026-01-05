using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class OpcDiscoveryResultDTO
    {
        public List<OpcDiscoveredServerDTO> Servers { get; set; } = new List<OpcDiscoveredServerDTO>();
        public TimeSpan ScanDuration { get; set; }
        public int TotalFound { get; set; }
        public string NetworkRange { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
    }
}

