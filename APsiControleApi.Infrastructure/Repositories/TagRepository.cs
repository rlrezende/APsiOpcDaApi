using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(APsiControleApiContext context) : base(context)
        {
        }

        // Métodos específicos para Tag podem ser implementados aqui
    }
}
