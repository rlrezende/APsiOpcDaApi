using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class OpcNodeRepository : GenericRepository<OpcNode>, IOpcNodeRepository
    {
        public OpcNodeRepository(APsiControleApiContext context) : base(context)
        {
        }
    }
}
