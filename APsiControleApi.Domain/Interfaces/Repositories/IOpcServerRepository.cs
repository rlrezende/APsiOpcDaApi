using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IOpcServerRepository : IGenericRepository<OpcServer>
    {
        Task<OpcServer?> GetByEndpointAsync(string endpoint);
        Task<IEnumerable<OpcServer>> GetServersByTypeAsync(TipoOpcServer tipo);
    }
}
