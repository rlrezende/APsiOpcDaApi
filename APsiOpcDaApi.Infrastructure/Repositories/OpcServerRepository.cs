using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class OpcServerRepository : GenericRepository<OpcServer>, IOpcServerRepository
    {
        public OpcServerRepository(APsiOpcDaApiContext context) : base(context)
        {
        }

        public async Task<OpcServer?> GetByEndpointAsync(string endpoint)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(server => server.Endpoint == endpoint);
        }

        public async Task<IEnumerable<OpcServer>> GetServersByTypeAsync(TipoOpcServer tipo)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(server => server.Tipo == tipo)
                .ToListAsync();
        }
    }
}

