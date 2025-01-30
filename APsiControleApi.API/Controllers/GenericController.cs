using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericController<TEntity, TDto> : ControllerBase
        where TEntity : class
        where TDto : class, IIdentifiable 
    {
        private readonly IGenericService<TEntity, TDto> _service;

        public GenericController(IGenericService<TEntity, TDto> service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entityDto = await _service.GetByIdAsync(id);
            if (entityDto == null)
            {
                return NotFound();
            }

            return Ok(entityDto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entitiesDto = await _service.GetAllAsync();
            return Ok(entitiesDto);
        }

        [HttpPost]
        public async Task<IActionResult> Add(TDto entityDto)
        {
            var createdEntityDto = await _service.AddAsync(entityDto);
            return CreatedAtAction(nameof(GetById), new { id = createdEntityDto }, createdEntityDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, TDto entityDto)
        {
            // Atribui o id diretamente ao DTO antes de enviá-lo ao serviço
            typeof(TDto).GetProperty("Id")?.SetValue(entityDto, id);

            // Chama o serviço para realizar a atualização
            await _service.UpdateAsync(entityDto);
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entityDto = await _service.GetByIdAsync(id);
            if (entityDto == null)
            {
                return NotFound();
            }

            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("paginadas")]
        public async Task<IActionResult> GetPaged(int pageIndex = 1, int pageSize = 10)
        {
            var (itemsDto, totalItems) = await _service.GetPagedAsync(pageIndex, pageSize);
            return Ok(new
            {
                totalItems,
                itemsDto
            });
        }
    }
}
