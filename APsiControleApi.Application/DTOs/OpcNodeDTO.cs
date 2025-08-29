using System;

namespace APsiControleApi.Application.DTOs
{
    public class OpcNodeDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public Guid ServerId { get; set; }
    }
}
