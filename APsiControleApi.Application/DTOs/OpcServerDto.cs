using APsiControleApi.Domain.Enum;
using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class OpcServerDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public Guid UnidadeId { get; set; }

        // Campos adicionados para refletir a entidade atualizada
        public TipoOpcServer Tipo { get; set; }
        public string? Provider { get; set; }
        public string? ConnectionString { get; set; }

        // Opcional: incluir os IDs dos nodes, conforme original
        public List<Guid> NodeIds { get; set; } = new List<Guid>();
    }
}
