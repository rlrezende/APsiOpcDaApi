using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : GenericController<Tag, TagDTO>
    {
        private readonly ITagService _tagService;

        public TagController(IGenericService<Tag, TagDTO> service, ITagService tagService)
            : base(service)
        {
            _tagService = tagService;
        }

        /// <summary>
        /// Retorna tags paginadas que possuem leituras associadas.
        /// </summary>
        /// <param name="pageIndex">Índice da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista de tags e o total de itens</returns>
        [HttpGet("paginadas-com-leituras")]
        public async Task<IActionResult> GetPagedTagsWithReadings([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            if (pageIndex < 1 || pageSize <= 0)
            {
                return BadRequest("Os parâmetros de paginação são inválidos.");
            }

            var (itemsDto, totalItems) = await _tagService.GetPagedTagsWithReadingsAsync(pageIndex, pageSize);

            return Ok(new { totalItems ,itemsDto });
        }
    }
}
