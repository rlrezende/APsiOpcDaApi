using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class LeituraRepository : GenericRepository<Leitura>, ILeituraRepository
    {
        public LeituraRepository(APsiControleApiContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém as leituras filtradas por unidade, período e tags.
        /// </summary>
        /// <param name="unidadeId">ID da unidade</param>
        /// <param name="dataInicio">Data inicial do período</param>
        /// <param name="dataFim">Data final do período</param>
        /// <param name="tagIds">Lista de IDs das tags</param>
        /// <returns>Lista de leituras filtradas</returns>
        public async Task<List<Leitura>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicioUtc, DateTime dataFimUtc, List<Guid> tagIds)
        {

                Instant dataInicio = Instant.FromDateTimeUtc(dataInicioUtc);
                Instant dataFim = Instant.FromDateTimeUtc(dataFimUtc); 
                return await (from leitura in _dbSet.AsNoTracking()
                    join tag in _context.Tag.AsNoTracking() on leitura.TagId equals tag.Id
                    where tag.UnidadeId == unidadeId &&
                            leitura.DataLeitura >= dataInicio &&
                            leitura.DataLeitura <= dataFim &&
                            (tagIds == null || !tagIds.Any() || tagIds.Contains(leitura.TagId))
                    select new Leitura
                    {
                        Id = leitura.Id,
                        DataLeitura = leitura.DataLeitura,
                        Valor = leitura.Valor,
                        TagId = leitura.TagId,
                        Tag = new Tag
                        {
                            Id = tag.Id,
                            Nome = tag.Nome,
                            UnidadeId = tag.UnidadeId,
                            Descricao = tag.Descricao
                        }
                    })
                    .ToListAsync();

    
        }

    /// <summary>
    /// Obtém as leituras das duas tags no período informado.
    /// </summary>
    /// <param name="unidadeId">ID da unidade</param>
    /// <param name="tagIds">Lista com os IDs das duas tags</param>
    /// <param name="dataInicio">Data inicial do período</param>
    /// <param name="dataFim">Data final do período</param>
    /// <returns>Lista de leituras (entidades)</returns>
    public async Task<List<Leitura>> ObterLeiturasSincronizadasEntreTagsAsync(
        Guid unidadeId, List<Guid> tagIds, DateTime dataInicioUtc, DateTime dataFimUtc)
    {
        Instant dataInicio = Instant.FromDateTimeUtc(dataInicioUtc);
        Instant dataFim = Instant.FromDateTimeUtc(dataFimUtc); 


        return await _dbSet
            .AsNoTracking()
            .Where(l => tagIds.Contains(l.TagId) &&
                        l.Tag.UnidadeId == unidadeId &&
                        l.DataLeitura >= dataInicio &&
                        l.DataLeitura <= dataFim)
            .OrderBy(l => l.DataLeitura)
            .ToListAsync();
    }

        // Métodos específicos para Leitura podem ser implementados aqui
    }
}
