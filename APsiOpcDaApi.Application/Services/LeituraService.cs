using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Services
{
    public class LeituraService : GenericService<Leitura, LeituraDTO>, ILeituraService
    {
        ILeituraRepository _repository;
        IMapper _mapper;

        public LeituraService(ILeituraRepository repository, IMapper mapper, IUserContextService userContextService)
            : base(repository, mapper, userContextService)
        {
            _repository = repository;
            _mapper = mapper;
        }

       public async Task ProcessarLeiturasAsync(List<ExcelWorksheet> planilhas, Guid unidadeId, Dictionary<int, Guid> tagMap)
        {
            const int batchSize = 500;  // Define o tamanho do lote
            var leiturasBatch = new List<LeituraDTO>();

            foreach (var planilha in planilhas)
            {
                var timestampCol = FindColumnByName(planilha, "Timestamp");
                if (timestampCol == 0)
                {
                    Console.WriteLine($"[Aviso] Planilha '{planilha.Name}' não possui a coluna 'Timestamp'. Ignorando...");
                    continue;
                }

                var linhas = planilha.Dimension.Rows;
                var colunas = planilha.Dimension.Columns;

                for (int i = 2; i <= linhas; i++)
                {
                    if (!DateTime.TryParse(planilha.Cells[i, timestampCol].Text, out var dataLeitura))
                    {
                        Console.WriteLine($"[Aviso] Linha {i}: Timestamp inválido. Ignorando...");
                        continue;
                    }

                    dataLeitura = DateTime.SpecifyKind(dataLeitura, DateTimeKind.Unspecified);

                    for (int col = 2; col <= colunas; col++)
                    {
                        if (col == timestampCol) continue;

                        if (!double.TryParse(planilha.Cells[i, col].Text, out var valor) || valor == 0.0) continue;

                        if (!int.TryParse(planilha.Cells[1, col].Text, out var tagIndex) || !tagMap.ContainsKey(tagIndex)) continue;

                        // Adiciona a leitura ao lote
                        leiturasBatch.Add(new LeituraDTO
                        {
                            DataLeitura = dataLeitura,
                            Valor = valor,
                            TagId = tagMap[tagIndex],
                        });

                        // Envia o lote ao banco de dados quando o limite for atingido
                        if (leiturasBatch.Count >= batchSize)
                        {
                            await AddRangeAsync(leiturasBatch);  // Chama o método genérico para inserção em lote
                            leiturasBatch.Clear();
                            leiturasBatch.TrimExcess();

                            // Força coleta de lixo após a inserção de alguns lotes grandes
                            if (GC.GetTotalMemory(false) > 500 * 1024 * 1024)  // 500 MB, por exemplo
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                }
            }

            // Insere o restante das leituras que não completaram o lote
            if (leiturasBatch.Any())
            {
                await AddRangeAsync(leiturasBatch);
            }
        }

                /// <summary>
        /// Retorna as leituras filtradas por unidade, período e tags.
        /// </summary>
        /// <param name="unidadeId">ID da unidade</param>
        /// <param name="dataInicio">Data inicial do período</param>
        /// <param name="dataFim">Data final do período</param>
        /// <param name="tagIds">IDs das tags a serem filtradas</param>
        /// <returns>Lista de leituras filtradas</returns>
        public async Task<List<LeituraDTO>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds)
        {
            var leituras = await _repository.ObterLeiturasPorPeriodoETagsAsync(unidadeId, dataInicio, dataFim, tagIds);
            return _mapper.Map<List<LeituraDTO>>(leituras);
        }

        /// <summary>
        /// Encontra o número da coluna com o nome especificado na primeira linha da planilha.
        /// </summary>
        /// <param name="planilha">Planilha Excel</param>
        /// <param name="nomeColuna">Nome da coluna a ser buscada</param>
        /// <returns>Número da coluna ou 0 se não for encontrada</returns>
        private int FindColumnByName(ExcelWorksheet planilha, string nomeColuna)
        {
            var colunas = planilha.Dimension.Columns;

            for (int col = 1; col <= colunas; col++)
            {
                if (planilha.Cells[1, col].Text.Equals(nomeColuna, StringComparison.OrdinalIgnoreCase))
                {
                    return col;
                }
            }

            return 0;
        }

        /// <summary>
        /// Obtém as leituras das duas tags no período informado, agrupadas por data.
        /// </summary>
        /// <param name="unidadeId">ID da unidade</param>
        /// <param name="tag1Id">ID da primeira tag</param>
        /// <param name="tag2Id">ID da segunda tag</param>
        /// <param name="dataInicio">Data inicial do período</param>
        /// <param name="dataFim">Data final do período</param>
        /// <returns>Lista de leituras filtradas e sincronizadas</returns>
        public async Task<List<LeituraDTO>> ObterLeiturasSincronizadasEntreTagsAsync(
            Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim)
        {
            var leituras = await ObterLeiturasPorPeriodoETagsAsync(
                unidadeId, dataInicio, dataFim, new List<Guid> { tag1Id, tag2Id });

            // Apenas retorna os dados; não faz cálculos aqui
            return leituras
                .Where(l => l.TagId == tag1Id || l.TagId == tag2Id)
                .OrderBy(l => l.DataLeitura)
                .ToList();
        }



    }
}

