using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(APsiControleApiContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Tag> items, int totalItems)> GetPagedTagsWithReadingsAsync(int pageIndex, int pageSize)
        {
            // Consulta para filtrar apenas tags que possuem leituras
            var query = from tag in _dbSet.AsNoTracking()
                        where _context.Leitura.Any(leitura => leitura.TagId == tag.Id)
                        select tag;

            // Contagem total após o filtro
            var totalItems = await query.CountAsync();

            // Paginando o resultado
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }


        // Métodos específicos para Tag podem ser implementados aqui
    }
}
