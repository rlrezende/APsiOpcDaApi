using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface ILeituraService : IGenericService<Leitura, LeituraDTO>
    {
        /// <summary>
        /// Processa uma lista de planilhas de leituras e insere os dados no banco.
        /// </summary>
        /// <param name="planilhas">Lista de planilhas Excel contendo as leituras</param>
        /// <param name="unidadeId">ID da unidade associada às leituras</param>
        /// <param name="tagMap">Dicionário de mapeamento das tags (Nome -> ID)</param>
        /// <returns>Tarefa assíncrona representando o processamento das leituras</returns>
        Task ProcessarLeiturasAsync(List<ExcelWorksheet> planilhas, Guid unidadeId,  Dictionary<int, Guid> tagMap);

        Task<List<LeituraDTO>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds);

        Task<List<LeituraDTO>> ObterLeiturasSincronizadasEntreTagsAsync(Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim);
    }
}

