using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class LeituraDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public DateTime DataLeitura { get; set; }  // Data em que a leitura foi realizada
        public double Valor { get; set; }  // Valor registrado na leitura

        // Relacionamento com a Tag
        public Guid TagId { get; set; }
        public string TagNome { get; set; }  // Nome da Tag associada, se necessário
    }
}