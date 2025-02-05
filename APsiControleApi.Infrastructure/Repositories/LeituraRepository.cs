using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;


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
        public async Task<List<Leitura>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds)
        {

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
        // Métodos específicos para Leitura podem ser implementados aqui
    }
}