using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class ControleDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;  // Nome do controle
        public string Descricao { get; set; } = string.Empty;  // Descrição do controle

        // Relacionamento com Unidade
        public Guid UnidadeId { get; set; }
        public string UnidadeNome { get; set; } = string.Empty;  // Nome da Unidade associada, se necessário
    }
}

