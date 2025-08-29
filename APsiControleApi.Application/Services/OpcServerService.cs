using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class OpcServerService : GenericService<OpcServer, OpcServerDTO>, IOpcServerService
    {
        private readonly IOpcServerRepository _opcServerRepository;
        private readonly IMapper _serviceMapper;

        public OpcServerService(
            IGenericRepository<OpcServer> repository, 
            IMapper mapper, 
            IUserContextService userContextService, 
            IOpcServerRepository opcServerRepository)
            : base(repository, mapper, userContextService)
        {
            _opcServerRepository = opcServerRepository;
            _serviceMapper = mapper;
        }

        public async Task<OpcServerDTO?> GetByEndpointAsync(string endpoint)
        {
            var server = await _opcServerRepository.GetByEndpointAsync(endpoint);
            return server != null ? _serviceMapper.Map<OpcServerDTO>(server) : null;
        }

        public async Task<IEnumerable<OpcServerDTO>> GetServersByTypeAsync(TipoOpcServer tipo)
        {
            var servers = await _opcServerRepository.GetServersByTypeAsync(tipo);
            return _serviceMapper.Map<IEnumerable<OpcServerDTO>>(servers);
        }
    }
}
