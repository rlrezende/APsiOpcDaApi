namespace APsiOpcDaApi.Domain.Entities
{
    public class Controle : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        // FK para Modulo, mantendo apenas o ID
        public Guid ModuloId { get; set; }

        protected Controle() { }
    }
}

