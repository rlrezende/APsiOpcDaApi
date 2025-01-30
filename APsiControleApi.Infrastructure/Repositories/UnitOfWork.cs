using APsiControleApi.Domain.Interfaces.Repositories;
using APsiControleApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;


namespace APsiControleApi.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly APsiControleApiContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(APsiControleApiContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}