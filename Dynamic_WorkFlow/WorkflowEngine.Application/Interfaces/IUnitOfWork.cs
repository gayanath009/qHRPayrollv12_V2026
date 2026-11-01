using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEngine.Domain.Common;

namespace WorkflowEngine.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
// Note: We inherit from IDisposable to cleanly close database context connections.
