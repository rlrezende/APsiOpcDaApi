using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpcNodeController : GenericController<OpcNode, OpcNodeDTO>
    {
        public OpcNodeController(IGenericService<OpcNode, OpcNodeDTO> service)
            : base(service)
        {
        }
    }
}
