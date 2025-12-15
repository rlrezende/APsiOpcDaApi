using System;
using System.Linq;
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

        public async Task<IEnumerable<Tag>> SearchTagsAsync(string? searchTerm, string? instrumentClass, Guid? groupId, int? limit = null)
        {
            var query = _context.Tag
                .AsNoTracking()
                .Include(tag => tag.Group)
                .AsQueryable();

            if (groupId.HasValue)
            {
                query = query.Where(tag => tag.GroupId == groupId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(tag => tag.Nome.ToLower().Contains(term) || tag.Descricao.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(instrumentClass))
            {
                if (instrumentClass.Equals("atuadores", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tag => tag.Nome.EndsWith(".MV") || tag.Nome.EndsWith(".CV") || tag.Nome.EndsWith(".FV"));
                }
                else if (instrumentClass.Equals("medidores", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tag => !tag.Nome.EndsWith(".MV") && !tag.Nome.EndsWith(".CV") && !tag.Nome.EndsWith(".FV"));
                }
            }

            query = query.OrderBy(tag => tag.Nome);

            if (limit.HasValue && limit.Value > 0)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            return await _context.Tag
                .AsNoTracking()
                .Where(tag => idList.Contains(tag.Id))
                .ToListAsync();
        }

        public async Task<Tag?> GetByNodeIdOpcAsync(string nodeIdOpc)
        {
            return await _context.Tag
                .Include(tag => tag.Group)
                .Include(tag => tag.Node)
                .FirstOrDefaultAsync(tag => tag.NodeIdOpc == nodeIdOpc);
        }
    }
}
