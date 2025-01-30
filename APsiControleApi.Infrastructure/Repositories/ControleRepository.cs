using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class ControleRepository : GenericRepository<Controle>, IControleRepository
    {
        public ControleRepository(APsiControleApiContext context) : base(context)
        {
        }

        // Métodos específicos para Controle podem ser implementados aqui
    }
}
