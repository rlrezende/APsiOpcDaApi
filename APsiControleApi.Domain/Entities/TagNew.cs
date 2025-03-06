namespace APsiControleApi.Domain.Entities
{
    public class TagNew : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string Endereco { get; set; }

        // FK para Unidade, apenas o ID
        public Guid UnidadeId { get; set; }

        // Propriedade de navegação virtual para lazy loading
        public virtual ICollection<Leitura> Leituras { get; set; }

        public TagNew() { }
    }
}
