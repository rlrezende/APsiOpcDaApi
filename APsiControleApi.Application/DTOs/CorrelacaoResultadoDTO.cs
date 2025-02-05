using System;

namespace APsiControleApi.Application.DTOs
{
    public class CorrelacaoResultadoDTO
    {
        public Guid Tag1Id { get; set; }       // ID da primeira tag
        public Guid Tag2Id { get; set; }       // ID da segunda tag
        public string Tag1Nome { get; set; }   // Nome da primeira tag (opcional)
        public string Tag2Nome { get; set; }   // Nome da segunda tag (opcional)
        public double ValorCorrelacao { get; set; }  // Valor da correlação entre as tags
    }
}
