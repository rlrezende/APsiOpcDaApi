using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APsiOpcDaApi.API.Controllers
{
    /// <summary>
    /// Controller para operações de manipulação web que requerem arquitetura x86
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class WebBrowserController : ControllerBase
    {
        private readonly IWebBrowserService _webBrowserService;

        public WebBrowserController(IWebBrowserService webBrowserService)
        {
            _webBrowserService = webBrowserService;
        }

        /// <summary>
        /// Verifica se o ambiente suporta operações x86
        /// </summary>
        /// <returns>Status do suporte x86</returns>
        [HttpGet("x86-support")]
        public IActionResult CheckX86Support()
        {
            try
            {
                var isSupported = _webBrowserService.IsX86Supported();
                return Ok(new 
                { 
                    isX86Supported = isSupported,
                    message = isSupported ? "Componentes x86 suportados" : "Componentes x86 não suportados nesta arquitetura",
                    environment = Environment.Is64BitProcess ? "x64" : "x86"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Navega para uma URL específica usando componentes x86
        /// </summary>
        /// <param name="url">URL para navegar</param>
        /// <returns>Resultado da navegação</returns>
        [HttpPost("navigate")]
        public async Task<IActionResult> NavigateToPage([FromBody] NavigationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest("URL é obrigatória");
            }

            try
            {
                var content = await _webBrowserService.NavigateToPageAsync(request.Url);
                return Ok(new { url = request.Url, content = content });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message, requiresX86 = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Executa JavaScript usando componentes x86
        /// </summary>
        /// <param name="request">Requisição com script</param>
        /// <returns>Resultado da execução</returns>
        [HttpPost("execute-script")]
        public async Task<IActionResult> ExecuteScript([FromBody] ScriptExecutionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest("Script é obrigatório");
            }

            try
            {
                var result = await _webBrowserService.ExecuteScriptAsync(request.Script);
                return Ok(new { script = request.Script, result = result });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message, requiresX86 = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Manipula elementos DOM usando componentes x86
        /// </summary>
        /// <param name="request">Requisição de manipulação</param>
        /// <returns>Resultado da manipulação</returns>
        [HttpPost("manipulate-dom")]
        public async Task<IActionResult> ManipulateDom([FromBody] DomManipulationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ElementId) || string.IsNullOrWhiteSpace(request.Action))
            {
                return BadRequest("ElementId e Action são obrigatórios");
            }

            try
            {
                await _webBrowserService.ManipulateDomElementAsync(request.ElementId, request.Action);
                return Ok(new { elementId = request.ElementId, action = request.Action, success = true });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message, requiresX86 = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTOs para as requisições
    public class NavigationRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public class ScriptExecutionRequest
    {
        public string Script { get; set; } = string.Empty;
    }

    public class DomManipulationRequest
    {
        public string ElementId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}