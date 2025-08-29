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

        public async Task<Guid?> GetOpcServerIdByTagIdAsync(Guid tagId)
        {
            return await _context.Tag
                .Where(t => t.Id == tagId)
                .Include(t => t.Node)
                .Select(t => t.Node.ServerId)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsByServerAsync(Guid serverId, string origem)
        {
            return await _context.Tag
                .AsNoTracking()
                .Where(tag => tag.Origem == origem && tag.Node.ServerId == serverId && tag.Monitora)
                .Include(tag => tag.Group)
                .ToListAsync();
        }



        // Métodos específicos para Tag podem ser implementados aqui
    }
}
