namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
