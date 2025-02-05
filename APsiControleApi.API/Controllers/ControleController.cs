using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AutoMapper;

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
        /// <param name="tagIds">Lista de IDs das tags a serem consideradas</param>
        /// <returns>Relatório de correlação entre as tags</returns>
        [HttpGet("relatorio-correlacao")]
        [AllowAnonymous]
        public async Task<IActionResult> GerarRelatorioCorrelacao(Guid unidadeId, DateTime dataInicio, DateTime dataFim, [FromQuery] List<Guid> tagIds)
        {
            if (dataInicio >= dataFim)
            {
                return BadRequest("A data de início deve ser anterior à data de fim.");
            }

            try
            {
                var correlacoes = await _controleService.GerarRelatorioCorrelacaoAsync(unidadeId, dataInicio, dataFim, tagIds);

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
