using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class LeituraDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public DateTime DataLeitura { get; set; }  // Data em que a leitura foi realizada
        public double Valor { get; set; }  // Valor registrado na leitura



        public string? ValorBruto { get; set; }
        public string? Erro { get; set; }


        // Relacionamento com a Tag
        public Guid TagId { get; set; }

        // Relacionamento completo com a Tag
        public TagDTO? Tag { get; set; }            // Objeto da Tag relacionada
    }
}
