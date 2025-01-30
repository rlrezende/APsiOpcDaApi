using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeituraController : GenericController<Leitura, LeituraDTO>
    {
        public LeituraController(IGenericService<Leitura, LeituraDTO> service)
            : base(service)
        {
        }

        // Métodos específicos para Leitura podem ser adicionados aqui, se necessário
    }
}
