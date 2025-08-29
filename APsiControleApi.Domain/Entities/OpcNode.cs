using System;

namespace APsiControleApi.Domain.Entities
{
    public class OpcNode : BaseEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;

        public Guid ServerId { get; set; }
        public virtual OpcServer? Server { get; set; }

        protected OpcNode() { }
    }
}
