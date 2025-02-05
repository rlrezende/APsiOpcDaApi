using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;
using OfficeOpenXml;

namespace APsiControleApi.Application.Services
{
    public class ControleService : GenericService<Controle, ControleDTO>, IControleService
    {
        private readonly ITagService _tagService;
        private readonly ILeituraService _leituraService;

        public ControleService(
            IGenericRepository<Controle> repository,
            IMapper mapper,
            IUserContextService userContextService,
            ITagService tagService,
            ILeituraService leituraService)
            : base(repository, mapper, userContextService)
        {
            _tagService = tagService;
            _leituraService = leituraService;
        }

        /// <summary>
        /// Gera um relatório de correlação entre as leituras das tags.
        /// </summary>
        /// <param name="unidadeId">ID da unidade</param>
        /// <param name="dataInicio">Data inicial do período</param>
        /// <param name="dataFim">Data final do período</param>
        /// <param name="tagIds">IDs das tags para análise</param>
        /// <returns>Relatório com os coeficientes de correlação</returns>
        public async Task<List<CorrelacaoResultadoDTO>> GerarRelatorioCorrelacaoAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds)
        {
                    // 1. Obter as leituras com as tags relacionadas
            var leituras = await _leituraService.ObterLeiturasPorPeriodoETagsAsync(unidadeId, dataInicio, dataFim, tagIds);

            // 2. Agrupar as leituras por tag
            var leiturasPorTag = leituras
                .GroupBy(l => l.TagId)
                .ToDictionary(g => g.Key, g => g.Select(l => (l.DataLeitura, l.Valor)).OrderBy(l => l.DataLeitura).ToArray());

            // 3. Calcular a correlação entre cada par de tags
            var relatorio = new List<CorrelacaoResultadoDTO>();

            var tags = leituras.Select(l => new { l.TagId, l.Tag.Nome }).Distinct().ToList();
            for (int i = 0; i < tags.Count; i++)
            {
                for (int j = i + 1; j < tags.Count; j++)
                {
                    var tag1 = tags[i];
                    var tag2 = tags[j];

                    // Sincronizar os valores das duas tags pelo timestamp
                    var valoresTag1 = leiturasPorTag[tag1.TagId].Select(l => l.Valor).ToArray();
                    var valoresTag2 = leiturasPorTag[tag2.TagId].Select(l => l.Valor).ToArray();

                    // Calcular a correlação
                    double correlacao = CalcularCorrelacao(valoresTag1, valoresTag2);

                    relatorio.Add(new CorrelacaoResultadoDTO
                    {
                        Tag1Id = tag1.TagId,
                        Tag2Id = tag2.TagId,
                        Tag1Nome = tag1.Nome,
                        Tag2Nome = tag2.Nome,
                        ValorCorrelacao = correlacao
                    });
                }
            }

            return relatorio;
        }


        /// <summary>
        /// Calcula o coeficiente de correlação entre dois arrays de valores.
        /// </summary>
        /// <param name="array1">Array de valores da primeira tag</param>
        /// <param name="array2">Array de valores da segunda tag</param>
        /// <returns>Coeficiente de correlação</returns>
        private double CalcularCorrelacao(double[] array1, double[] array2)
        {
            int n = Math.Min(array1.Length, array2.Length);

            double sumX = 0, sumY = 0, sumXY = 0;
            double sumX2 = 0, sumY2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += array1[i];
                sumY += array2[i];
                sumXY += array1[i] * array2[i];
                sumX2 += array1[i] * array1[i];
                sumY2 += array2[i] * array2[i];
            }

            double stdX = Math.Sqrt(sumX2 / n - (sumX / n) * (sumX / n));
            double stdY = Math.Sqrt(sumY2 / n - (sumY / n) * (sumY / n));
            double covariance = (sumXY / n) - (sumX / n) * (sumY / n);

            return stdX > 0 && stdY > 0 ? covariance / (stdX * stdY) : 0.0;
        }



        

        public async Task ProcessarArquivoExcelAsync(Stream arquivoStream, Guid unidadeId)
        {
            // Definir o contexto de licença para evitar erro
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var pacote = new ExcelPackage(arquivoStream);

            // Carrega as planilhas necessárias
            var planilhaControles = pacote.Workbook.Worksheets["Controls"];
            var planilhaTags = pacote.Workbook.Worksheets["Tags"];
            var planilhasLeituras = pacote.Workbook.Worksheets
                              .Where(w => DateTime.TryParse(w.Name, out _))
                              .ToList();
            if (planilhaControles == null || planilhaTags == null || planilhasLeituras == null)
            {
                throw new InvalidOperationException("O arquivo Excel precisa conter as planilhas 'Controls', 'Tags' e uma planilha de leituras.");
            }

            // 1. Processar e inserir as tags
            var tagMap = await _tagService.ProcessarTagsAsync(planilhaTags, unidadeId);

            // 2. Processar os controles
            await ProcessarControlesAsync(planilhaControles, unidadeId);

            // 3. Processar as leituras associadas às tags
            await _leituraService.ProcessarLeiturasAsync(planilhasLeituras, unidadeId, tagMap);
        }

        private async Task ProcessarControlesAsync(ExcelWorksheet planilha, Guid unidadeId)
        {
            if (planilha?.Dimension == null || planilha.Dimension.Rows <= 1)
            {
                throw new InvalidOperationException("A planilha de controles não contém dados suficientes.");
            }

            var linhas = planilha.Dimension.Rows;

            for (int i = 2; i <= linhas; i++)
            {
                try
                {
                    // Validação e limpeza dos campos obrigatórios
                    var nome = planilha.Cells[i, 2].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(nome))
                    {
                        Console.WriteLine($"[Aviso] Linha {i}: O campo 'Nome' está vazio. Registro ignorado.");
                        continue;
                    }

                    var descricao = planilha.Cells[i, 3].Text?.Trim();

                    // Criação do DTO baseado nos dados validados
                    var controleDto = new ControleDTO
                    {
                        Nome = nome,
                        Descricao = descricao,
                        UnidadeId = unidadeId
                    };

                    // Verifica duplicidade no banco antes de inserir
                    var controleExistente = await GetByConditionAsync(c => c.Nome == nome && c.UnidadeId == unidadeId);
                    if (controleExistente != null)
                    {
                        Console.WriteLine($"[Aviso] Linha {i}: O controle '{nome}' já existe. Registro ignorado.");
                        continue;
                    }

                    // Insere o controle usando o serviço genérico
                    await AddAsync(controleDto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erro] Linha {i}: Erro ao processar o controle. Detalhes: {ex.Message}");
                }
            }
        }
    }
}
