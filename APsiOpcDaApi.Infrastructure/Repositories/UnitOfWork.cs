using APsiOpcDaApi.Domain.Interfaces.Repositories;
using APsiOpcDaApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;


namespace APsiOpcDaApi.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly APsiOpcDaApiContext _context;
        private IDbContextTransaction? _transaction;
        private bool _ownsTransaction;
        private string? _savepoint;

        public UnitOfWork(APsiOpcDaApiContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction is not null)
            {
                throw new InvalidOperationException("Já existe uma transação ativa nesta unidade de trabalho.");
            }

            _transaction = _context.Database.CurrentTransaction;
            _ownsTransaction = _transaction is null;
            if(_ownsTransaction)_transaction = await _context.Database.BeginTransactionAsync();
            else
            {
                _savepoint="uow_"+Guid.NewGuid().ToString("N");
                await _transaction!.CreateSavepointAsync(_savepoint);
            }
        }

        public async Task CommitAsync()
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException("Nenhuma transação ativa para commit.");
            }

            await _context.SaveChangesAsync();
            if(_ownsTransaction)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
            }
            else await _transaction.ReleaseSavepointAsync(_savepoint!);
            _savepoint=null;
            _transaction = null;
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                if(_ownsTransaction)
                {
                    await _transaction.RollbackAsync();
                    await _transaction.DisposeAsync();
                }
                else
                {
                    await _transaction.RollbackToSavepointAsync(_savepoint!);
                    await _transaction.ReleaseSavepointAsync(_savepoint!);
                }
                _savepoint=null;
                _transaction = null;
                _context.ChangeTracker.Clear();
            }
        }

        public void Dispose()
        {
            if(_ownsTransaction)_transaction?.Dispose();
            else if(_transaction is not null && _savepoint is not null)
            {
                _transaction.RollbackToSavepoint(_savepoint);
                _transaction.ReleaseSavepoint(_savepoint);
                _context.ChangeTracker.Clear();
            }
            _transaction=null;_savepoint=null;
        }
    }
}
