using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class TagDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }  // Nome da Tag
        public string Descricao { get; set; }  // Descrição da Tag

         public int idOld { get; set; }  

        // Novo campo para associar a unidade
        public Guid UnidadeId { get; set; }

        // Relacionamentos, se aplicável
        public ICollection<Guid> LeituraIds { get; set; }  // IDs de leituras associadas
    }
}
