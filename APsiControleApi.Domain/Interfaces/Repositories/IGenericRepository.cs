using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IGenericRepository<TEntity>
        where TEntity : class
    {
        Task<TEntity> GetByIdAsync(Guid id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(Guid id);
        Task<(IEnumerable<TEntity> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize);
    }
}
