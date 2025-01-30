using System;
using System.Collections.Generic;
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
        private readonly IMapper _mapper;
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

        public async Task<TDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return _mapper.Map<TDto>(entity);
        }

        public async Task<IEnumerable<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TDto>>(entities);
        }

        public async Task<TDto> AddAsync(TDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);

            // Define a data de criação
            var createdDateProperty = typeof(TEntity).GetProperty("CreatedDate");
            if (createdDateProperty != null)
            {
                createdDateProperty.SetValue(entity, DateTime.UtcNow);
            }

            // Atribui o EmpresaId, se necessário
            var empresaId = _userContextService.GetEmpresaId();
            if (empresaId.HasValue)
            {
                SetEmpresaIdIfExists(entity, empresaId.Value);
            }

            await _repository.AddAsync(entity);
            return _mapper.Map<TDto>(entity);
        }

        public async Task UpdateAsync(TDto dto) 
        {
            var existingEntity = await _repository.GetByIdAsync(dto.Id);
            if (existingEntity == null)
            {
                throw new InvalidOperationException("Entidade não encontrada.");
            }

            // Atualiza a entidade com os dados do DTO
            _mapper.Map(dto, existingEntity);

            // Define a data de atualização
            var updatedDateProperty = typeof(TEntity).GetProperty("UpdatedDate");
            if (updatedDateProperty != null)
            {
                updatedDateProperty.SetValue(existingEntity, DateTime.UtcNow);
            }

            // Atribui o EmpresaId, se necessário
            var empresaId = _userContextService.GetEmpresaId();
            if (empresaId.HasValue)
            {
                SetEmpresaIdIfExists(existingEntity, empresaId.Value);
            }

            await _repository.UpdateAsync(existingEntity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<(IEnumerable<TDto> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize)
        {
            var (entities, totalItems) = await _repository.GetPagedAsync(pageIndex, pageSize);
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);
            return (dtos, totalItems);
        }

        private void SetEmpresaIdIfExists(TEntity entity, Guid empresaId)
        {
            var empresaIdProperty = typeof(TEntity).GetProperty("EmpresaId");
            if (empresaIdProperty != null && empresaIdProperty.PropertyType == typeof(Guid))
            {
                empresaIdProperty.SetValue(entity, empresaId);
            }
        }
    }
}
