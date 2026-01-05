using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;
using Microsoft.FSharp.Data.UnitSystems.SI.UnitNames;
using OfficeOpenXml;

namespace APsiOpcDaApi.Application.Services
{
    public class DadosCorrelacao
    {
        public DateTime DataLeitura { get; set; }
        public double ValorTag1 { get; set; }
        public double ValorTag2 { get; set; }
    }

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
        /// <param name="metodo">metodo considerado para o calculo da correlacao</param>
        /// <param name="AtrasoMax">Atraso maximo considerado para o calculo da correlacao</param>
        /// <returns>Relatório com os coeficientes de correlação</returns>
        public async Task<List<CorrelacaoResultadoDTO>> GerarRelatorioCorrelacaoAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds, MetodoCorrelacao metodo, TimeSpan AtrasoMax)
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
                    // var valoresTag1 = leiturasPorTag[tag1.TagId].Select(l => l.Valor).ToArray();
                    // var valoresTags = leiturasPorTag[tag1.TagId].Select(l => (l.DataLeitura, l.Valor)).ToArray(),leiturasPorTag[tag2.TagId].Select(l => l.Valor).ToArray();

                    var leiturasTag1 = leiturasPorTag[tag1.TagId].Select(l => new { l.DataLeitura, l.Valor }).ToList();
                    var leiturasTag2 = leiturasPorTag[tag2.TagId].Select(l => new { l.DataLeitura, l.Valor }).ToList();

                    var dadosCorrelacao = leiturasTag1
                        .Join(leiturasTag2, 
                            l1 => l1.DataLeitura, 
                            l2 => l2.DataLeitura, 
                            (l1, l2) => new DadosCorrelacao 
                            {
                                DataLeitura = l1.DataLeitura,
                                ValorTag1 = l1.Valor,
                                ValorTag2 = l2.Valor
                            })
                        .ToList();

                    // Calcular a correlação fazer case
                    TimeSpan BestLag = TimeSpan.Zero;;
                    double BestCorrelation=0;
                    int BestSamples=dadosCorrelacao.Count;
                    switch (metodo)
                     {
                         case MetodoCorrelacao.Pearson:
                             (BestLag, BestCorrelation,BestSamples) = CalcularCorrelacaoPearson(dadosCorrelacao, AtrasoMax);
                             // Lógica para o calculo da correlacao Pearson
                             break;
                         case MetodoCorrelacao.Spearman:
                             (BestLag, BestCorrelation,BestSamples) = CalcularCorrelacaoSpearman(dadosCorrelacao, AtrasoMax);
                             // Lógica para o calculo da correlacao Spearman
                             break;
                         case MetodoCorrelacao.Kendall:
                             (BestLag, BestCorrelation,BestSamples)  = CalcularCorrelacaoKendall(dadosCorrelacao, AtrasoMax);
                             // Lógica para o calculo da correlacao Kendall
                             break;
                         default:
                             BestLag = TimeSpan.Zero;;
                             BestCorrelation = 0;
                             BestSamples=dadosCorrelacao.Count;
                             break;
                     }
                    

                    relatorio.Add(new CorrelacaoResultadoDTO
                    {
                        Tag1Id = tag1.TagId,
                        Tag2Id = tag2.TagId,
                        Tag1Nome = tag1.Nome,
                        Tag2Nome = tag2.Nome,
                        ValorCorrelacao = BestCorrelation,
                        ValorAtraso = BestLag,
                        ValorAmostras = BestSamples
                    });
                }
            }

            return relatorio;
        }

        public int EncontrarIndicePorTimestamp(List<DadosCorrelacao> dados, TimeSpan deslocamentoTempo)
        {
            if (dados.Count == 0)
                return -1; // Retorna -1 se a lista estiver vazia

            DateTime dataInicial = dados[0].DataLeitura;
            DateTime dataAlvo = dataInicial + deslocamentoTempo;

            for (int i = 0; i < dados.Count; i++)
            {
                if (dados[i].DataLeitura >= dataAlvo)
                    return i;
            }

            return -1; // Retorna -1 se nenhum elemento satisfizer a condição
        }

        /// <summary>
        /// Calcula o coeficiente de correlação entre dois arrays de valores.
        /// </summary>
        /// <param name="dadosCorrelacao">Array de valores da primeira tag, segunda tag e o atraso mãximo</param>
        /// <returns> Atraso e o coeficiente de correlação</returns>
        private (TimeSpan, double,int)  CalcularCorrelacaoPearson(List<DadosCorrelacao> dadosCorrelacao, TimeSpan AtrasoMax)
        {
            TimeSpan BestLag = TimeSpan.Zero;
            double BestCorrelation = 0;
            
            
            int MaxLag=EncontrarIndicePorTimestamp(dadosCorrelacao,AtrasoMax);
            int BestSamples=dadosCorrelacao.Count;
            for (int Lag = 0; Lag <= MaxLag; Lag++)
            {
                //valoresTag1.Where(l => valoresTag2.Any(t2 => t2.DataLeitura == l.DataLeitura - atraso)).Select(l => l.Valor).ToArray();                
                double sumX = 0, sumY = 0, sumXY = 0;
                double sumX2 = 0, sumY2 = 0;
                int n=dadosCorrelacao.Count-Lag;
                for (int i = 0; i < n; i++)
                {
                    sumX += dadosCorrelacao[i].ValorTag1;
                    sumY += dadosCorrelacao[i+Lag].ValorTag2;
                    sumXY += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i+Lag].ValorTag2;
                    sumX2 += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i].ValorTag1;
                    sumY2 += dadosCorrelacao[i+Lag].ValorTag2 *dadosCorrelacao[i+Lag].ValorTag2;
                }
                double stdX = Math.Sqrt(sumX2 / n - (sumX / n) * (sumX / n));
                double stdY = Math.Sqrt(sumY2 / n - (sumY / n) * (sumY / n));
                double covariance = (sumXY / n) - (sumX / n) * (sumY / n);
                double Correlation = stdX > 0 && stdY > 0 ? covariance / (stdX * stdY) : 0.0;
                if (Math.Abs(Correlation) > Math.Abs(BestCorrelation))
                    {
                        BestSamples = dadosCorrelacao.Count - n;
                        BestCorrelation = Correlation;
                        BestLag =  dadosCorrelacao[Lag].DataLeitura - dadosCorrelacao[0].DataLeitura;
                    }
            }
            return (BestLag, BestCorrelation, BestSamples);
        }

        /// <summary>
        /// Calcula o coeficiente de correlação entre dois arrays de valores.
        /// </summary>
        /// <param name="dadosCorrelacao">Array de valores da primeira tag, segunda tag e o atraso mãximo</param>
        /// <returns> Atraso e o coeficiente de correlação</returns>
        private (TimeSpan, double,int)   CalcularCorrelacaoSpearman(List<DadosCorrelacao> dadosCorrelacao, TimeSpan AtrasoMax)
        {
            TimeSpan BestLag = TimeSpan.Zero;
            double BestCorrelation = 0;
            
            
            int MaxLag=EncontrarIndicePorTimestamp(dadosCorrelacao,AtrasoMax);
            int BestSamples=dadosCorrelacao.Count;
            for (int Lag = 0; Lag <= MaxLag; Lag++)
            {
                //valoresTag1.Where(l => valoresTag2.Any(t2 => t2.DataLeitura == l.DataLeitura - atraso)).Select(l => l.Valor).ToArray();                
                double sumX = 0, sumY = 0, sumXY = 0;
                double sumX2 = 0, sumY2 = 0;
                int n=dadosCorrelacao.Count-Lag;
                for (int i = 0; i < n; i++)
                {
                    sumX += dadosCorrelacao[i].ValorTag1;
                    sumY += dadosCorrelacao[i+Lag].ValorTag2;
                    sumXY += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i+Lag].ValorTag2;
                    sumX2 += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i].ValorTag1;
                    sumY2 += dadosCorrelacao[i+Lag].ValorTag2 *dadosCorrelacao[i+Lag].ValorTag2;
                }
                double stdX = Math.Sqrt(sumX2 / n - (sumX / n) * (sumX / n));
                double stdY = Math.Sqrt(sumY2 / n - (sumY / n) * (sumY / n));
                double covariance = (sumXY / n) - (sumX / n) * (sumY / n);
                double Correlation = stdX > 0 && stdY > 0 ? covariance / (stdX * stdY) : 0.0;
                if (Math.Abs(Correlation) > Math.Abs(BestCorrelation))
                    {
                        BestSamples = dadosCorrelacao.Count - n;
                        BestCorrelation = Correlation;
                        BestLag =  dadosCorrelacao[Lag].DataLeitura - dadosCorrelacao[0].DataLeitura;
                    }
            }
            return (BestLag, BestCorrelation, BestSamples);
        }

        public async Task<CorrelacaoGraficoDTO> ObterRelatorioDeCorrelacaoAsync(
            Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim)
        {
            // 1️⃣ Obter os dados do LeituraService
            var leituras = await _leituraService.ObterLeiturasSincronizadasEntreTagsAsync(
                unidadeId, tag1Id, tag2Id, dataInicio, dataFim);

            if (!leituras.Any())
                throw new InvalidOperationException("Nenhuma leitura encontrada para as tags e período informados.");

            // 2️⃣ Sincronizar leituras por data
            var pontosSincronizados = leituras
                .GroupBy(l => l.DataLeitura)
                .Where(g => g.Select(l => l.TagId).Distinct().Count() == 2) // Apenas datas com ambas as tags
                .Select(g => new PontoLeituraDTO
                {
                    DataLeitura = g.Key,
                    ValorTag1 = g.FirstOrDefault(l => l.TagId == tag1Id)?.Valor ?? 0,
                    ValorTag2 = g.FirstOrDefault(l => l.TagId == tag2Id)?.Valor ?? 0
                })
                .OrderBy(p => p.DataLeitura)
                .ToList();

            if (!pontosSincronizados.Any())
                throw new InvalidOperationException("Nenhum ponto sincronizado encontrado entre as duas tags.");

            

            // 4️⃣ Recuperar nomes das tags
            var tag1Nome = leituras.FirstOrDefault(l => l.TagId == tag1Id)?.Tag?.Nome ?? "Tag 1";
            var tag2Nome = leituras.FirstOrDefault(l => l.TagId == tag2Id)?.Tag?.Nome ?? "Tag 2";

            // 5️⃣ Montar e retornar o DTO final
            return new CorrelacaoGraficoDTO
            {
                Tag1Id = tag1Id,
                Tag1Nome = tag1Nome,
                Tag2Id = tag2Id,
                Tag2Nome = tag2Nome,
                Pontos = pontosSincronizados
            };
        }
        
        /// <summary>
        /// Calcula o coeficiente de correlação entre dois arrays de valores.
        /// </summary>
        /// <param name="dadosCorrelacao">Array de valores da primeira tag, segunda tag e o atraso mãximo</param>
        /// <returns> Atraso e o coeficiente de correlação</returns>
        private (TimeSpan, double,int)   CalcularCorrelacaoKendall(List<DadosCorrelacao> dadosCorrelacao, TimeSpan AtrasoMax)
        {
            TimeSpan BestLag = TimeSpan.Zero;
            double BestCorrelation = 0;
            
            
            int MaxLag=EncontrarIndicePorTimestamp(dadosCorrelacao,AtrasoMax);
            int BestSamples=dadosCorrelacao.Count;
            for (int Lag = 0; Lag <= MaxLag; Lag++)
            {
                //valoresTag1.Where(l => valoresTag2.Any(t2 => t2.DataLeitura == l.DataLeitura - atraso)).Select(l => l.Valor).ToArray();                
                double sumX = 0, sumY = 0, sumXY = 0;
                double sumX2 = 0, sumY2 = 0;
                int n=dadosCorrelacao.Count-Lag;
                for (int i = 0; i < n; i++)
                {
                    sumX += dadosCorrelacao[i].ValorTag1;
                    sumY += dadosCorrelacao[i+Lag].ValorTag2;
                    sumXY += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i+Lag].ValorTag2;
                    sumX2 += dadosCorrelacao[i].ValorTag1 * dadosCorrelacao[i].ValorTag1;
                    sumY2 += dadosCorrelacao[i+Lag].ValorTag2 *dadosCorrelacao[i+Lag].ValorTag2;
                }
                double stdX = Math.Sqrt(sumX2 / n - (sumX / n) * (sumX / n));
                double stdY = Math.Sqrt(sumY2 / n - (sumY / n) * (sumY / n));
                double covariance = (sumXY / n) - (sumX / n) * (sumY / n);
                double Correlation = stdX > 0 && stdY > 0 ? covariance / (stdX * stdY) : 0.0;
                if (Math.Abs(Correlation) > Math.Abs(BestCorrelation))
                    {
                        BestSamples = dadosCorrelacao.Count - n;
                        BestCorrelation = Correlation;
                        BestLag =  dadosCorrelacao[Lag].DataLeitura - dadosCorrelacao[0].DataLeitura;
                    }
            }
            return (BestLag, BestCorrelation, BestSamples);
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

