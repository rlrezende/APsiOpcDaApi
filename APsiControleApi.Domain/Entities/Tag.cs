namespace APsiControleApi.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }

        // FK para Unidade
        public Guid UnidadeId { get; set; }
        public virtual Unidade Unidade { get; set; } // Propriedade virtual para lazy loading

        // Propriedade de navegação virtual para lazy loading
        public virtual ICollection<Leitura> Leituras { get; set; }

        // Construtor protegido ou público para uso com lazy loading proxies
        protected Tag() { } // ou use public Tag() {}
    }
}
