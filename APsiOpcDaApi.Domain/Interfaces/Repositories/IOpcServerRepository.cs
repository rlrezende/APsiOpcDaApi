using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;

namespace APsiOpcDaApi.Domain.Interfaces.Repositories
{
    public interface IOpcServerRepository : IGenericRepository<OpcServer>
    {
        Task<OpcServer?> GetByEndpointAsync(string endpoint);
        Task<OpcServer?> GetByEndpointAndModuloIdAsync(string endpoint, Guid moduloId);
        Task<IEnumerable<OpcServer>> GetServersByTypeAsync(TipoOpcServer tipo);
    }
}
