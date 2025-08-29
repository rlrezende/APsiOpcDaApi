using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace APsiControleApi.Infrastructure.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly APsiControleApiContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(APsiControleApiContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<TEntity> GetByConditionAsync(Expression<Func<TEntity, bool>> condition)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(condition);
        }

        public async Task AddAsync(TEntity entity)
        {
            AttachRelatedEntities(entity);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            // Anexa as entidades relacionadas
            foreach (var entity in entities)
            {
                AttachRelatedEntities(entity);
            }

            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity); // mais seguro para chaves estrangeiras
            await _context.SaveChangesAsync(); 
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("Entity not found");
            }
        }

        public async Task<(IEnumerable<TEntity> items, int totalItems)> GetPagedAsync(int pageIndex, int pageSize)
        {
            var totalItems = await _dbSet.CountAsync();
            var items = await _dbSet.AsNoTracking()
                                    .Skip((pageIndex - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            return (items, totalItems);
        }

        private void AttachRelatedEntities(TEntity entity)
        {
            var entityEntry = _context.Entry(entity);

            foreach (var navigationEntry in entityEntry.Collections)
            {
                if (navigationEntry.CurrentValue is IEnumerable<object> relatedEntities)
                {
                    foreach (var relatedEntity in relatedEntities)
                    {
                        var relatedEntry = _context.Entry(relatedEntity);
                        if (relatedEntry.State == EntityState.Detached)
                        {
                            relatedEntry.State = EntityState.Unchanged;
                        }
                    }
                }
            }
        }
    }
}
