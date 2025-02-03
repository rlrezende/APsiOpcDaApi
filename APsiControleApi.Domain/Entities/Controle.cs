namespace APsiControleApi.Domain.Entities
{
    public class Controle : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }

        // FK para Unidade, mantendo apenas o ID
        public Guid UnidadeId { get; set; }

        protected Controle() { }
    }
}
