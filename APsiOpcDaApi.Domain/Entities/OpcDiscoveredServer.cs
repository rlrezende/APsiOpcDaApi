using System;

namespace APsiOpcDaApi.Domain.Entities
{
    public class OpcDiscoveredServer : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ApplicationUri { get; set; } = string.Empty;
        public DateTime DiscoveryTime { get; set; }
        public bool IsOnline { get; set; }
        public string SecurityModes { get; set; } = string.Empty; // JSON array
        public string NetworkRange { get; set; } = string.Empty;
        public int ResponseTime { get; set; } // ms
        
        protected OpcDiscoveredServer() { }
    }
}

