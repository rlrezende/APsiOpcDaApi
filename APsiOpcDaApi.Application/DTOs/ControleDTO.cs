using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class ControleDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;  // Nome do controle
        public string Descricao { get; set; } = string.Empty;  // Descrição do controle

        // Relacionamento com Módulo
        public Guid ModuloId { get; set; }
        public string ModuloNome { get; set; } = string.Empty;  // Nome do Módulo associado, se necessário
    }
}

