using APsiOpcDaApi.Domain.Enum;
using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class OpcServerDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public Guid ModuloId { get; set; }
        public TipoOpcServer Tipo { get; set; }
        public string? Descricao { get; set; }
        public string? SecurityPolicy { get; set; }
        public string? SecurityMode { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Host { get; set; }
        public string? ProgId { get; set; }
        public string? ClsId { get; set; }
        public string? Provider { get; set; }
        public string? ConnectionString { get; set; }
        public bool IsConnected { get; set; }
        public DateTime? LastConnection { get; set; }
        public DateTime? DiscoveryTime { get; set; }
        public bool IsOnline { get; set; }
        public string? ConnectionStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public int ResponseTime { get; set; }
        public List<Guid> NodeIds { get; set; } = new List<Guid>();
    }
}

