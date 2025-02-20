using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using System;
using System.IO;
using System.Threading.Tasks;

namespace APsiControleApi.Application.Interfaces
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

         Task<List<CorrelacaoResultadoDTO>> GerarRelatorioCorrelacaoAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds, MetodoCorrelacao metodo);
    }
}
