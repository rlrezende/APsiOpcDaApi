using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class OpcServerRepository : GenericRepository<OpcServer>, IOpcServerRepository
    {
        public OpcServerRepository(APsiControleApiContext context) : base(context)
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
