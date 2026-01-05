using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class ControleRepository : GenericRepository<Controle>, IControleRepository
    {
        public ControleRepository(APsiOpcDaApiContext context) : base(context)
        {
        }

        // Métodos específicos para Controle podem ser implementados aqui
    }
}

