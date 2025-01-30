namespace APsiControleApi.Domain.Entities
{
    public class Controle : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }

        // FK para Unidade
        public Guid UnidadeId { get; set; }
        public virtual Unidade Unidade { get; set; } // Propriedade virtual para lazy loading

        // Construtor protegido ou público para uso com lazy loading proxies
        protected Controle() { } // ou use public Controle() {}
    }
}
