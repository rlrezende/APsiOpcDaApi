using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class LeituraRepository : GenericRepository<Leitura>, ILeituraRepository
    {
        public LeituraRepository(APsiOpcDaApiContext context) : base(context)
        {
        }

        public async Task<List<Leitura>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicioUtc, DateTime dataFimUtc, List<Guid> tagIds)
        {
            var dataInicio = Instant.FromDateTimeUtc(dataInicioUtc);
            var dataFim = Instant.FromDateTimeUtc(dataFimUtc);

            var tagsQuery = _context.Tag
                .AsNoTracking()
                .Where(t => t.ModuloId == unidadeId);

            if (tagIds is { Count: > 0 })
            {
                tagsQuery = tagsQuery.Where(t => tagIds.Contains(t.Id));
            }

            var tagInfos = await tagsQuery
                .Select(t => new { t.Id, t.Nome, t.ModuloId, t.Descricao })
                .ToListAsync();

            if (tagInfos.Count == 0)
            {
                return new List<Leitura>();
            }

            var tagInfoById = tagInfos.ToDictionary(t => t.Id);
            var tagIdsValidos = tagInfos.Select(t => t.Id).ToList();

            var leituras = await _dbSet
                .AsNoTracking()
                .Where(l => tagIdsValidos.Contains(l.TagId)
                            && l.DataLeitura >= dataInicio
                            && l.DataLeitura <= dataFim)
                .Select(l => new { l.Id, l.DataLeitura, l.Valor, l.TagId })
                .OrderBy(l => l.DataLeitura)
                .ToListAsync();

            return leituras
                .Select(l =>
                {
                    var tag = tagInfoById[l.TagId];
                    return new Leitura
                    {
                        Id = l.Id,
                        DataLeitura = l.DataLeitura,
                        Valor = l.Valor,
                        TagId = l.TagId,
                        Tag = new Tag
                        {
                            Id = tag.Id,
                            Nome = tag.Nome,
                            ModuloId = tag.ModuloId,
                            Descricao = tag.Descricao
                        }
                    };
                })
                .ToList();
        }

        public async Task<List<Leitura>> ObterLeiturasSincronizadasEntreTagsAsync(
            Guid unidadeId, List<Guid> tagIds, DateTime dataInicioUtc, DateTime dataFimUtc)
        {
            var dataInicio = Instant.FromDateTimeUtc(dataInicioUtc);
            var dataFim = Instant.FromDateTimeUtc(dataFimUtc);

            if (tagIds == null || tagIds.Count == 0)
            {
                return new List<Leitura>();
            }

            var tagIdsValidos = await _context.Tag
                .AsNoTracking()
                .Where(t => t.ModuloId == unidadeId && tagIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync();

            if (tagIdsValidos.Count == 0)
            {
                return new List<Leitura>();
            }

            return await _dbSet
                .AsNoTracking()
                .Where(l => tagIdsValidos.Contains(l.TagId)
                            && l.DataLeitura >= dataInicio
                            && l.DataLeitura <= dataFim)
                .OrderBy(l => l.DataLeitura)
                .ToListAsync();
        }
    }
}
