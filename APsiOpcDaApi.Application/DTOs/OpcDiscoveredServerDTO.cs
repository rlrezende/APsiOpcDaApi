using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class OpcDiscoveredServerDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ApplicationUri { get; set; } = string.Empty;
        public DateTime DiscoveryTime { get; set; }
        public bool IsOnline { get; set; }
        public List<string> SecurityModes { get; set; } = new List<string>();
        public int ResponseTime { get; set; }
        public string NetworkRange { get; set; } = string.Empty;
    }
}

