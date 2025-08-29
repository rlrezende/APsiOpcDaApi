using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class OpcNodeService : GenericService<OpcNode, OpcNodeDTO>, IOpcNodeService
    {
        public OpcNodeService(IGenericRepository<OpcNode> repository, IMapper mapper, IUserContextService userContextService)
            : base(repository, mapper, userContextService)
        {
        }
    }
}
