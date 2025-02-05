using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface ILeituraRepository : IGenericRepository<Leitura>
    {
        Task<List<Leitura>> ObterLeiturasPorPeriodoETagsAsync(Guid unidadeId, DateTime dataInicio, DateTime dataFim, List<Guid> tagIds);
        
    }
}
