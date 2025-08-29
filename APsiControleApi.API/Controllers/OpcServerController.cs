using APsiControleApi.API.Controllers;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpcServerController : GenericController<OpcServer,OpcServerDTO>
    {
        public OpcServerController(IGenericService<OpcServer, OpcServerDTO> service)
            : base(service)
        {
        }
    }
}
