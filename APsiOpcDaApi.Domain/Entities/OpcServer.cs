using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Domain.Entities
{
    public class OpcServer : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint para OPC UA ou CLSID/ProgID no caso de DA
        /// Para Database, pode estar vazio
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        public Guid ModuloId { get; set; }
        [NotMapped]
        public Guid UnidadeId { get => ModuloId; set => ModuloId = value; }
        public string? Descricao { get; set; }

        /// <summary>
        /// Tipo de servidor: OPC UA, DA ou Database
        /// </summary>
        public TipoOpcServer Tipo { get; set; }

        public string? SecurityPolicy { get; set; }
        public string? SecurityMode { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Host { get; set; }
        public string? ProgId { get; set; }
        public string? ClsId { get; set; }
        // Configurações adicionais apenas para Database
        public string? Provider { get; set; } // ex: "SqlServer", "PostgreSQL"
        public string? ConnectionString { get; set; }

        // Status de conexão / monitoramento
        public bool IsConnected { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public DateTime? LastConnection { get; set; }
        public DateTime? DiscoveryTime { get; set; }
        public bool IsOnline { get; set; } = false;
        public string? ConnectionStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public int ResponseTime { get; set; } = 0;

        public virtual ICollection<OpcNode> Nodes { get; set; } = new List<OpcNode>();
        public virtual ICollection<OpcGroup> Groups { get; set; } = new List<OpcGroup>();

        protected OpcServer() { }
    }
}

