using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Common;
using WorkflowEngine.Infrastructure.Persistence.Repositories;

namespace WorkflowEngine.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly WorkflowDbContext _context;
    private IDbContextTransaction? _transaction;
    private Hashtable? _repositories;

    public UnitOfWork(WorkflowDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        if (_repositories == null)
            _repositories = new Hashtable();

        var type = typeof(T).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(GenericRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IGenericRepository<T>)_repositories[type]!;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
