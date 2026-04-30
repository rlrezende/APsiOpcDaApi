using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public class TagDTO : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        public int idOld { get; set; }

        // Campo para associar o módulo
        public Guid ModuloId { get; set; }

        // Opcional: identificador interno
        public Guid? NodeId { get; set; }

        // ✅ Campo que indica se a tag deve ser monitorada
        public bool Monitora { get; set; }

        // ✅ Valor atual lido da tag
        public double? ValorAtual { get; set; }

        // ✅ Identificação do grupo OPC
        public Guid? GroupId { get; set; }

        // ✅ Lista de leituras relacionadas
        public ICollection<Guid> LeituraIds { get; set; } = new List<Guid>();

        // ✅ Novo campo: NodeId real OPC UA (ex: "ns=2;s=MyDevice.Tag1")
        public string? NodeIdOpc { get; set; }

        public string Origem { get; set; } = "OPCUA"; // "OPCUA" ou "Database"

        public string? NomeTabela { get; set; }
        public string? NomeColuna { get; set; }
    }
}

