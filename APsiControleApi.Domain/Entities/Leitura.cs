namespace APsiControleApi.Domain.Entities
{
    public class Leitura : BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime DataLeitura { get; set; }
        public double Valor { get; set; }

        // FK para Tag
        public Guid TagId { get; set; }
        public virtual Tag Tag { get; set; } // Propriedade virtual para lazy loading

        // Construtor protegido ou público para uso com lazy loading proxies
        protected Leitura() { } // ou use public Leitura() {}
    }
}
