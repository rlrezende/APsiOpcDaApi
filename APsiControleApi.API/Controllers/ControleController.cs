using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AutoMapper;
using APsiControleApi.Domain.Enum;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControleController : GenericController<Controle, ControleDTO>
    {
        private readonly IControleService _controleService;
        private readonly IMapper _mapper;

        public ControleController(IControleService controleService, IMapper mapper)
            : base(controleService)
        {
            _controleService = controleService;
            _mapper = mapper;
        }


            /// <summary>
            /// Gera um relatório de correlação entre duas tags em um período específico.
            /// </summary>
            /// <param name="unidadeId">ID da unidade</param>
            /// <param name="tag1Id">ID da primeira tag</param>
            /// <param name="tag2Id">ID da segunda tag</param>
            /// <param name="dataInicio">Data de início do período</param>
            /// <param name="dataFim">Data de fim do período</param>
            /// <returns>Relatório de correlação com os pontos sincronizados</returns>
            [HttpGet("relatorio-correlacao-entre-tags")]
            [AllowAnonymous]
            public async Task<IActionResult> GerarRelatorioCorrelacaoEntreTags(
                Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim)
            {
                if (dataInicio >= dataFim)
                {
                    return BadRequest("A data de início deve ser anterior à data de fim.");
                }

                try
                {
                    var correlacao = await _controleService.ObterRelatorioDeCorrelacaoAsync(
                        unidadeId, tag1Id, tag2Id, dataInicio, dataFim);

                    return Ok(correlacao);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Erro ao gerar o relatório: {ex.Message}");
                }
            }

        /// <summary>
        /// Endpoint para fazer o upload de um arquivo Excel e processar os dados.
        /// </summary>
        /// <param name="arquivo">Arquivo Excel enviado via formulário</param>
        /// <returns>Retorna status de sucesso ou erro</returns>
        [HttpPost("upload-excel")]
        [AllowAnonymous] 
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado ou o arquivo está vazio.");
            }

            try
            {
                using var stream = arquivo.OpenReadStream();
                await _controleService.ProcessarArquivoExcelAsync(stream, new Guid("7f9ab23c-9860-4daa-9489-e5806b9f63d1"));
                return Ok("Arquivo processado e dados inseridos com sucesso.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao processar o arquivo: {ex.Message}");
            }
        }

         /// <summary>
        /// Gera um relatório de correlação entre tags e valores em um período específico.
        /// </summary>
        /// <param name="unidadeId">ID da unidade</param>
        /// <param name="dataInicio">Data de início do período</param>
        /// <param name="dataFim">Data de fim do período</param>
        /// <param name="metodo">Metodo utilizado para o calculo da correlacao</param>
        /// <param name="AtrasoMax">Atraso maximo para considerar no calculo da correlacao</param>
        /// <param name="tagIds">Lista de IDs das tags a serem consideradas</param>
        /// <returns>Relatório de correlação entre as tags</returns>
        [HttpGet("relatorio-correlacao")]
        [AllowAnonymous]
        public async Task<IActionResult> GerarRelatorioCorrelacao(Guid unidadeId, DateTime dataInicio, DateTime dataFim,int metodo, TimeSpan AtrasoMax, [FromQuery] List<Guid> tagIds)
        {
            if (dataInicio >= dataFim)
            {
                return BadRequest("A data de início deve ser anterior à data de fim.");
            }

            try
            {
                var correlacoes = await _controleService.GerarRelatorioCorrelacaoAsync(unidadeId, dataInicio, dataFim, tagIds, (MetodoCorrelacao)metodo, AtrasoMax);

            // Transformar o dicionário em uma lista de DTOs manualmente


        return Ok(correlacoes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao gerar o relatório: {ex.Message}");
            }
        }


    }
}
