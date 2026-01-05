namespace APsiOpcDaApi.Domain.Entities
{
    public class TagNew : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;

        // FK para Unidade, apenas o ID
        public Guid UnidadeId { get; set; }

        // Propriedade de navegação virtual para lazy loading
        public virtual ICollection<Leitura> Leituras { get; set; } = new List<Leitura>();

        public TagNew() { }
    }
}

