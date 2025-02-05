using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IGenericRepository<TEntity>
        where TEntity : class
    {
        Task<TEntity> GetByIdAsync(Guid id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);  // Método para inserção em lote
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(Guid id);
        Task<(IEnumerable<TEntity> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize);

        Task<TEntity> GetByConditionAsync(Expression<Func<TEntity, bool>> condition);
    }
}
