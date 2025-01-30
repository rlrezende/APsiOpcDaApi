using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControleController : GenericController<Controle, ControleDTO>
    {
        public ControleController(IGenericService<Controle, ControleDTO> service)
            : base(service)
        {
        }

        // Métodos específicos para Controle podem ser adicionados aqui, se necessário
    }
}