using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class ControleDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }  // Nome do controle
        public string Descricao { get; set; }  // Descrição do controle

        // Relacionamento com Unidade
        public Guid UnidadeId { get; set; }
        public string UnidadeNome { get; set; }  // Nome da Unidade associada, se necessário
    }
}
