using APsiOpcDaApi.Domain.Interfaces.Repositories;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;


namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly APsiOpcDaApiContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(APsiOpcDaApiContext context)
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

