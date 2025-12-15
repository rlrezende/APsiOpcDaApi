using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcServerService : IGenericService<OpcServer, OpcServerDTO>
    {
        Task<OpcServerDTO?> GetByEndpointAsync(string endpoint);
        Task<IEnumerable<OpcServerDTO>> GetServersByTypeAsync(TipoOpcServer tipo);
        bool IsOpcDaSupported();
    }
}
