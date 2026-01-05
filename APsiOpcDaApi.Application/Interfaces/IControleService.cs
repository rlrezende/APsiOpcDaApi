using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;

using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IControleService : IGenericService<Controle, ControleDTO>
    {
        /// <summary>
        /// Processa um arquivo Excel para inserir dados relacionados a Controle, Tags e Leituras.
        /// </summary>
        /// <param name="arquivo">Stream do arquivo Excel</param>
        /// <param name="unidadeId">ID da unidade associada aos dados</param>
        /// <returns>Tarefa assíncrona representando o processamento do arquivo</returns>
        Task ProcessarArquivoExcelAsync(Stream arquivo, Guid unidadeId);
        Task<CorrelacaoGraficoDTO> ObterRelatorioDeCorrelacaoAsync(Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim);
        Task<List<CorrelacaoResultadoDTO>> GerarRelatorioCorrelacaoAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds, MetodoCorrelacao metodo, TimeSpan AtrasoMax);
    }
}

