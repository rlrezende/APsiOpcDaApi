using NodaTime;

namespace APsiOpcDaApi.Domain.Entities
{
    public class Leitura : BaseEntity
    {
        public Guid Id { get; set; }
        public Instant DataLeitura { get; set; }
        public double Valor { get; set; }


        // NOVOS CAMPOS
        public string? ValorBruto { get; set; }
        public string? Erro { get; set; }

        // FK para Tag
        public Guid TagId { get; set; }
        public virtual Tag? Tag { get; set; } // Propriedade virtual para lazy loading

        // Construtor protegido ou público para uso com lazy loading proxies
        public Leitura() { } // ou use public Leitura() {}
    }
}

