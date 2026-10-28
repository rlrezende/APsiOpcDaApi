using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using AutoMapper;

namespace APsiOpcDaApi.Application.Services
{
    public class GenericService<TEntity, TDto> : IGenericService<TEntity, TDto>
        where TEntity : class
        where TDto : class, IIdentifiable 
    {
        private readonly IGenericRepository<TEntity> _repository;
        protected readonly IMapper _mapper;
        protected readonly IUserContextService _userContextService;

        protected bool InScope(object entity) => entity != null && _userContextService.HasAccessToResource(entity);
        protected void DemandScope(object entity) { if (!InScope(entity)) throw new APsiOpcDaApi.Application.Exceptions.ResourceScopeDeniedException(); }

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
            if (entity != null) DemandScope(entity);
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TDto>>(entities.Where(e => InScope(e)));
        }

        public virtual async Task<TDto> AddAsync(TDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            SetCreatedDate(entity);
            SetEmpresaIdIfExists(entity);
            DemandScope(entity);

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
            DemandScope(entity);
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

            DemandScope(existingEntity);
            PrepareUpdate(dto,existingEntity);
            // Validate a detached destination first: a rejected DTO must never
            // poison an EF-tracked source and leak into a later SaveChanges.
            var destination = _mapper.Map<TEntity>(dto);
            SetEmpresaIdIfExists(destination);
            DemandScope(destination);
            _mapper.Map(dto, existingEntity);
            DemandScope(existingEntity);
            SetUpdatedDate(existingEntity);
            SetEmpresaIdIfExists(existingEntity);

            await _repository.UpdateAsync(existingEntity);
        }

        protected virtual void PrepareUpdate(TDto dto,TEntity existingEntity) { }

        public virtual async Task DeleteAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            DemandScope(existing);
            await _repository.DeleteAsync(id);
        }

        public virtual async Task<(IEnumerable<TDto> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize)
        {
            if (pageIndex < 1 || pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            var visible = (await _repository.GetAllAsync()).Where(e => InScope(e)).ToArray();
            return (_mapper.Map<IEnumerable<TDto>>(visible.Skip((pageIndex - 1) * pageSize).Take(pageSize)), visible.Length);
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
            var entity = await _repository.GetByConditionAsync(condition);
            if (entity != null) DemandScope(entity);
            return entity;
        }
    }
}

