using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class OpcNodeRepository : GenericRepository<OpcNode>, IOpcNodeRepository
    {
        public OpcNodeRepository(APsiOpcDaApiContext context) : base(context)
        {
        }
    }
}

