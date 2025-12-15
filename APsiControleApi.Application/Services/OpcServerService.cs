using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly bool _isWindows;

        public OpcServerService(
            IGenericRepository<OpcServer> repository, 
            IMapper mapper, 
            IUserContextService userContextService, 
            IOpcServerRepository opcServerRepository)
            : base(repository, mapper, userContextService)
        {
            _opcServerRepository = opcServerRepository;
            _serviceMapper = mapper;
            _isWindows = OperatingSystem.IsWindows();
        }

        public bool IsOpcDaSupported() => _isWindows;

        public override async Task<IEnumerable<OpcServerDTO>> GetAllAsync()
        {
            var servers = await base.GetAllAsync();
            if (_isWindows)
            {
                return servers;
            }

            return servers.Where(s => s.Tipo != TipoOpcServer.Da);
        }

        public override async Task<OpcServerDTO> AddAsync(OpcServerDTO dto)
        {
            ValidateEnvironment(dto);
            NormalizeFields(dto);
            return await base.AddAsync(dto);
        }

        public override async Task UpdateAsync(OpcServerDTO dto)
        {
            ValidateEnvironment(dto);
            NormalizeFields(dto);
            await base.UpdateAsync(dto);
        }

        public async Task<OpcServerDTO?> GetByEndpointAsync(string endpoint)
        {
            var server = await _opcServerRepository.GetByEndpointAsync(endpoint);
            return server != null ? _serviceMapper.Map<OpcServerDTO>(server) : null;
        }

        public async Task<IEnumerable<OpcServerDTO>> GetServersByTypeAsync(TipoOpcServer tipo)
        {
            if (!_isWindows && tipo == TipoOpcServer.Da)
            {
                return Enumerable.Empty<OpcServerDTO>();
            }

            var servers = await _opcServerRepository.GetServersByTypeAsync(tipo);
            return _serviceMapper.Map<IEnumerable<OpcServerDTO>>(servers);
        }

        private void ValidateEnvironment(OpcServerDTO dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (!_isWindows && dto.Tipo == TipoOpcServer.Da)
            {
                throw new InvalidOperationException("Servidores OPC DA só podem ser configurados em ambientes Windows.");
            }
        }

        private static void NormalizeFields(OpcServerDTO dto)
        {
            if (dto.Tipo == TipoOpcServer.Da)
            {
                if (string.IsNullOrWhiteSpace(dto.Endpoint) && !string.IsNullOrWhiteSpace(dto.Host))
                {
                    dto.Endpoint = dto.Host;
                }
            }
        }
    }
}
