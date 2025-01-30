using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : GenericController<Tag, TagDTO>
    {
        public TagController(IGenericService<Tag, TagDTO> service)
            : base(service)
        {
        }

        // Métodos específicos para Tag podem ser adicionados aqui, se necessário
    }
}
