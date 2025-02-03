using System;

namespace APsiControleApi.Application.DTOs
{
    public class UnidadeDto : IIdentifiable
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Endereco { get; set; }

        // FK para Empresa
        public Guid EmpresaId { get; set; }

        // Opcional: Nome da Empresa para exibir no frontend
        public string NomeEmpresa { get; set; }
    }
}
