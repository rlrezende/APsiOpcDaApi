using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class LeituraRepository : GenericRepository<Leitura>, ILeituraRepository
    {
        public LeituraRepository(APsiControleApiContext context) : base(context)
        {
        }

        // Métodos específicos para Leitura podem ser implementados aqui
    }
}