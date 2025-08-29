using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.Entities
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

        public Guid UnidadeId { get; set; }

        /// <summary>
        /// Tipo de servidor: OPC UA, DA ou Database
        /// </summary>
        public TipoOpcServer Tipo { get; set; }

        // Configurações adicionais apenas para Database
        public string? Provider { get; set; } // ex: "SqlServer", "PostgreSQL"
        public string? ConnectionString { get; set; }

        public virtual ICollection<OpcNode> Nodes { get; set; } = new List<OpcNode>();

        protected OpcServer() { }
    }
}
