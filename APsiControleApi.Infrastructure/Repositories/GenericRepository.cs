using APsiControleApi.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        private readonly APsiControleApiContext _context;
        private readonly DbSet<TEntity> _dbSet;

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
            return await _dbSet.ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            // Verifica as coleções de navegação e anexa entidades que já existem
            foreach (var entry in _context.Entry(entity).Collections)
            {
                if (entry.CurrentValue != null)
                {
                    foreach (var relatedEntity in (IEnumerable<object>)entry.CurrentValue)
                    {
                        var relatedEntry = _context.Entry(relatedEntity);
                        if (relatedEntry.State == EntityState.Detached)
                        {
                            // Marca como Unchanged para evitar inserção de entidades já existentes
                            relatedEntry.State = EntityState.Unchanged;
                        }
                    }
                }
            }

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }


        public async Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
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
            var items = await _dbSet.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalItems);
        }
    }
}
