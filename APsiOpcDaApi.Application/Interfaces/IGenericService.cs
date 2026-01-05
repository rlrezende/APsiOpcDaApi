using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IGenericService<TEntity, TDto>
        where TEntity : class
        where TDto : class, IIdentifiable
    {
        Task<TDto> GetByIdAsync(Guid id);
        Task<IEnumerable<TDto>> GetAllAsync();
        Task<TDto> AddAsync(TDto dto);
        Task AddRangeAsync(IEnumerable<TDto> dtos);  // Novo método para inserção em lote
        Task UpdateAsync(TDto dto);
        Task DeleteAsync(Guid id);
        Task<(IEnumerable<TDto> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize);
        Task<TEntity> GetByConditionAsync(Expression<Func<TEntity, bool>> condition);
    }
}

