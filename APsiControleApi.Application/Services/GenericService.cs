using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiControleApi.Application.Services
{
    public class GenericService<TEntity, TDto> : IGenericService<TEntity, TDto>
        where TEntity : class
        where TDto : class, IIdentifiable 
    {
        private readonly IGenericRepository<TEntity> _repository;
        protected readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public GenericService(
            IGenericRepository<TEntity> repository,
            IMapper mapper,
            IUserContextService userContextService)
        {
            _repository = repository;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public virtual async Task<TDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TDto>>(entities);
        }

        public virtual async Task<TDto> AddAsync(TDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            SetCreatedDate(entity);
            SetEmpresaIdIfExists(entity);

            await _repository.AddAsync(entity);
            return _mapper.Map<TDto>(entity);
        }

        public async Task AddRangeAsync(IEnumerable<TDto> dtos)
        {
            var entities = _mapper.Map<IEnumerable<TEntity>>(dtos);

            foreach (var entity in entities)
            {
                SetCreatedDate(entity);
                SetEmpresaIdIfExists(entity);
            }

            await _repository.AddRangeAsync(entities);
        }

        public virtual async Task UpdateAsync(TDto dto)
        {
            var existingEntity = await _repository.GetByIdAsync(dto.Id);
            if (existingEntity == null)
            {
                throw new InvalidOperationException("Entidade não encontrada.");
            }

            _mapper.Map(dto, existingEntity);
            SetUpdatedDate(existingEntity);
            SetEmpresaIdIfExists(existingEntity);

            await _repository.UpdateAsync(existingEntity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        public virtual async Task<(IEnumerable<TDto> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize)
        {
            var (entities, totalItems) = await _repository.GetPagedAsync(pageIndex, pageSize);
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);
            return (dtos, totalItems);
        }

        private void SetCreatedDate(TEntity entity)
        {
            var createdDateProperty = typeof(TEntity).GetProperty("CreatedDate");
            if (createdDateProperty != null)
            {
                createdDateProperty.SetValue(entity, DateTime.UtcNow);
            }
        }

        private void SetUpdatedDate(TEntity entity)
        {
            var updatedDateProperty = typeof(TEntity).GetProperty("UpdatedDate");
            if (updatedDateProperty != null)
            {
                updatedDateProperty.SetValue(entity, DateTime.UtcNow);
            }
        }

        private void SetEmpresaIdIfExists(TEntity entity)
        {
            var empresaIdProperty = typeof(TEntity).GetProperty("EmpresaId");
            if (empresaIdProperty != null && empresaIdProperty.PropertyType == typeof(Guid))
            {
                var empresaId = _userContextService.GetEmpresaId();
                if (empresaId.HasValue)
                {
                    empresaIdProperty.SetValue(entity, empresaId.Value);
                }
            }
        }

        public async Task<TEntity> GetByConditionAsync(Expression<Func<TEntity, bool>> condition)
        {
            return await _repository.GetByConditionAsync(condition);
        }
    }
}
