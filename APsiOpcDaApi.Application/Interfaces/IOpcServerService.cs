using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcServerService : IGenericService<OpcServer, OpcServerDTO>
    {
        Task<OpcServerDTO?> GetByEndpointAsync(string endpoint);
        Task<OpcServerDTO?> GetByEndpointAndModuloIdAsync(string endpoint, Guid moduloId);
        Task<IEnumerable<OpcServerDTO>> GetServersByTypeAsync(TipoOpcServer tipo);
        bool IsOpcDaSupported();
    }
}
