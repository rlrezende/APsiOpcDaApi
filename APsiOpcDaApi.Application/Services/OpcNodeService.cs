using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiOpcDaApi.Application.Services
{
    public class OpcNodeService : GenericService<OpcNode, OpcNodeDTO>, IOpcNodeService
    {
        public OpcNodeService(IGenericRepository<OpcNode> repository, IMapper mapper, IUserContextService userContextService)
            : base(repository, mapper, userContextService)
        {
        }
    }
}

