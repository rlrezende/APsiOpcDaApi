using System;

namespace APsiControleApi.Application.DTOs
{
    public class OpcConnectionStatusDTO
    {
        public Guid ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime? LastConnection { get; set; }
        public int ResponseTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
