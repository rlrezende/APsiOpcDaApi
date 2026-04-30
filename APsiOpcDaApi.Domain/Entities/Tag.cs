namespace APsiOpcDaApi.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        public int idOld { get; set; }

        public Guid ModuloId { get; set; }

        public Guid? NodeId { get; set; }
        public virtual OpcNode? Node { get; set; }

        public Guid? GroupId { get; set; }
        public virtual OpcGroup? Group { get; set; }

        public double? ValorAtual { get; set; }

        public bool Monitora { get; set; }

        public string? NodeIdOpc { get; set; }

        public string Origem { get; set; } = "OPCUA"; // "OPCUA" ou "Database"

        public string? NomeTabela { get; set; }
        public string? NomeColuna { get; set; }

        public virtual ICollection<Leitura> Leituras { get; set; } = new List<Leitura>();

        public Tag() { }
    }
}

