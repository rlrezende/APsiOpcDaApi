using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public class CorrelacaoGraficoDTO : IIdentifiable
    {
        public Guid Id { get; set; }

        // Informações da primeira tag
        public Guid Tag1Id { get; set; }
        public string Tag1Nome { get; set; } = string.Empty;
        public string Tag1Descricao { get; set; } = string.Empty;

        // Informações da segunda tag
        public Guid Tag2Id { get; set; }
        public string Tag2Nome { get; set; } = string.Empty;
        public string Tag2Descricao { get; set; } = string.Empty;

        // Valor da correlação calculada
        public double ValorCorrelacao { get; set; }

        // Pontos sincronizados para plotar no gráfico
        public ICollection<PontoLeituraDTO> Pontos { get; set; } = new List<PontoLeituraDTO>();
    }

    public class PontoLeituraDTO
    {
        public DateTime DataLeitura { get; set; }  // Data da leitura
        public double ValorTag1 { get; set; }      // Valor da primeira tag
        public double ValorTag2 { get; set; }      // Valor da segunda tag
    }
}
