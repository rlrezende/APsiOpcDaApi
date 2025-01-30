using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs; // Importa o namespace onde IIdentifiable está definido

namespace APsiControleApi.Application.Interfaces
{
    public interface IGenericService<TEntity, TDto>
        where TEntity : class
        where TDto : class, IIdentifiable // Adiciona a restrição para garantir que TDto tenha Id
    {
        Task<TDto> GetByIdAsync(Guid id);
        Task<IEnumerable<TDto>> GetAllAsync();
        Task<TDto> AddAsync(TDto dto);
        Task UpdateAsync(TDto dto);
        Task DeleteAsync(Guid id);
        Task<(IEnumerable<TDto> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize);
    }
}
