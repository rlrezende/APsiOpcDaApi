using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiOpcDaApi.Application.Services
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
            return servers.Where(s => s.Tipo == TipoOpcServer.Da);
        }

        public override async Task<OpcServerDTO> GetByIdAsync(Guid id)
        {
            var server = await base.GetByIdAsync(id);
            return server?.Tipo == TipoOpcServer.Da ? server : null!;
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
            if (server != null) DemandScope(server);
            if (server?.Tipo != TipoOpcServer.Da) return null;
            return server != null ? _serviceMapper.Map<OpcServerDTO>(server) : null;
        }

        public async Task<OpcServerDTO?> GetByEndpointAndModuloIdAsync(string endpoint, Guid moduloId)
        {
            var server = await _opcServerRepository.GetByEndpointAndModuloIdAsync(endpoint, moduloId);
            if (server != null) DemandScope(server);
            if (server?.Tipo != TipoOpcServer.Da) return null;
            return server != null ? _serviceMapper.Map<OpcServerDTO>(server) : null;
        }

        public async Task<IEnumerable<OpcServerDTO>> GetServersByTypeAsync(TipoOpcServer tipo)
        {
            if (tipo != TipoOpcServer.Da)
            {
                return Enumerable.Empty<OpcServerDTO>();
            }

            var servers = await _opcServerRepository.GetServersByTypeAsync(tipo);
            return _serviceMapper.Map<IEnumerable<OpcServerDTO>>(servers.Where(s => InScope(s)));
        }

        protected override void PrepareUpdate(OpcServerDTO dto,OpcServer existing)
        {
            if(dto.ClearPassword && !string.IsNullOrEmpty(dto.Password) || dto.ClearConnectionString && !string.IsNullOrEmpty(dto.ConnectionString))
                throw new ArgumentException("AMBIGUOUS_CREDENTIAL_CHANGE");
            dto.Password=dto.ClearPassword?null:string.IsNullOrEmpty(dto.Password)?existing.Password:dto.Password;
            dto.ConnectionString=dto.ClearConnectionString?null:string.IsNullOrEmpty(dto.ConnectionString)?existing.ConnectionString:dto.ConnectionString;
        }

        private void ValidateEnvironment(OpcServerDTO dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.Tipo != TipoOpcServer.Da) throw new InvalidOperationException("PROTOCOL_OWNER_MISMATCH");
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
