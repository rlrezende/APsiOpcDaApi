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
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class ControleController : GenericController<Controle, ControleDTO>
    {
        private readonly IControleService _controleService;
        private readonly IMapper _mapper;
        private readonly ISimuladorLeituraService _simuladorLeituraService;

        public ControleController(
            IControleService controleService,
            IMapper mapper,
            ISimuladorLeituraService simuladorLeituraService)
            : base(controleService)
        {
            _controleService = controleService;
            _mapper = mapper;
            _simuladorLeituraService = simuladorLeituraService;
        }

        [HttpGet("relatorio-correlacao-entre-tags")]
        [AllowAnonymous]
        public async Task<IActionResult> GerarRelatorioCorrelacaoEntreTags(
            Guid unidadeId, Guid tag1Id, Guid tag2Id, DateTime dataInicio, DateTime dataFim)
        {
            if (dataInicio >= dataFim)
                return BadRequest("A data de início deve ser anterior à data de fim.");

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

        [HttpPost("upload-excel")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Nenhum arquivo foi enviado ou o arquivo está vazio.");

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

        [HttpGet("relatorio-correlacao")]
        [AllowAnonymous]
        public async Task<IActionResult> GerarRelatorioCorrelacao(Guid unidadeId, DateTime dataInicio, DateTime dataFim, int metodo, long atrasoMaxMs, [FromQuery] List<Guid> tagIds)
        {
            if (dataInicio >= dataFim)
                return BadRequest("A data de início deve ser anterior à data de fim.");

            try
            {
                var atrasoMax = TimeSpan.FromMilliseconds(atrasoMaxMs);
                var correlacoes = await _controleService.GerarRelatorioCorrelacaoAsync(unidadeId, dataInicio, dataFim, tagIds, (MetodoCorrelacao)metodo, atrasoMax);
                return Ok(correlacoes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao gerar o relatório: {ex.Message}");
            }
        }

        [HttpPost("iniciar-simulacao")]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSimulacao([FromBody] SimulacaoRequest request)
        {
            await _simuladorLeituraService.IniciarSimulacaoAsync(request.TagIds, request.UnidadeId);
            return Ok("Simulação histórica iniciada.");
        }

        [HttpPost("iniciar-simulacao-pid-resposta")]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSimulacaoPidRespostaAoDegrau([FromBody] SimulacaoPidRequest request)
        {
            await _simuladorLeituraService.IniciarSimulacaoPIDComRespostaAoDegrauAsync(
                request.K,
                request.Tau,
                request.Theta,
                request.TagKp,
                request.TagKi,
                request.TagKd,
                request.OutrasTags,
                request.UnidadeId,
                request.ValorInicial
            );

            return Ok("Simulação PID com resposta ao degrau iniciada.");
        }

        [HttpPost("iniciar-simulacao-pid-oscilacao")]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSimulacaoPidOscilacao([FromBody] SimulacaoPidOscilacaoRequest request)
        {
            await _simuladorLeituraService.IniciarSimulacaoPIDOscilacaoSustentadaAsync(
                request.Ku,
                request.Pu,
                request.TagKp,
                request.TagKi,
                request.TagKd,
                request.UnidadeId
            );

            return Ok("Simulação PID (Oscilação Sustentada) iniciada.");
        }

        [HttpPost("iniciar-simulacao-pid-sintese-direta")]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSimulacaoPidSinteseDireta([FromBody] SimulacaoPidSinteseDiretaRequest request)
        {
            await _simuladorLeituraService.IniciarSimulacaoPIDSinteseDiretaAsync(
                request.K,
                request.Tau,
                request.Theta,
                request.Taud,
                request.TagKp,
                request.TagKi,
                request.TagKd,
                request.UnidadeId
            );

            return Ok("Simulação PID (Síntese Direta) iniciada.");
        }

    }

    public class SimulacaoPidOscilacaoRequest
{
    public Guid UnidadeId { get; set; }
    public double Ku { get; set; }
    public double Pu { get; set; }
    public Guid? TagKp { get; set; }
    public Guid? TagKi { get; set; }
    public Guid? TagKd { get; set; }
}


        public class SimulacaoPidSinteseDiretaRequest
        {
            public Guid UnidadeId { get; set; }
            public double K { get; set; }
            public double Tau { get; set; }
            public double Theta { get; set; }
            public double Taud { get; set; }
            public Guid? TagKp { get; set; }
            public Guid? TagKi { get; set; }
            public Guid? TagKd { get; set; }
        }


    public class SimulacaoRequest
    {
        public Guid UnidadeId { get; set; }
        public List<Guid> TagIds { get; set; }
    }

    public class SimulacaoPidRequest
    {
        public Guid UnidadeId { get; set; }
        public double K { get; set; }
        public double Tau { get; set; }
        public double Theta { get; set; }
        public Guid? TagKp { get; set; }
        public Guid? TagKi { get; set; }
        public Guid? TagKd { get; set; }
        public List<Guid> OutrasTags { get; set; }
        public double? ValorInicial { get; set; }
    }
}
