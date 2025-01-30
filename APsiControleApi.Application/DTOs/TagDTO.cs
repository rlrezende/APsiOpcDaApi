using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class TagDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }  // Nome da Tag
        public string Descricao { get; set; }  // Descrição da Tag

        // IDs relacionados, caso aplicável
        public ICollection<Guid> LeituraIds { get; set; }  // Exemplo de relacionamentos
    }
}
