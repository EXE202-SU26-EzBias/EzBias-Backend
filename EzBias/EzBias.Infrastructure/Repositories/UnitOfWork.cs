using EzBias.Domain.Exceptions;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EzBias.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly EzBiasDbContext _db;

    public UnitOfWork(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The record was modified concurrently.",
                ex);
        }
        catch (DbUpdateException ex) when (IsFandomWriteConflict(ex))
        {
            throw new FandomWriteConflictException("A conflicting fandom was created concurrently.", ex);
        }
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
            return new ExistingTransaction();

        var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public void ClearTrackedChanges() => _db.ChangeTracker.Clear();

    private static bool IsFandomWriteConflict(DbUpdateException exception)
    {
        var postgresException = exception.InnerException as PostgresException;
        return postgresException?.ConstraintName is "ux_fandoms_normalized_name" or "PK_fandoms";
    }

    private sealed class ExistingTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _completed;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_completed) return;
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_completed) return;
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                catch
                {
                    // Preserve the original exception, if any, while still
                    // disposing the database transaction.
                }
            }

            await _transaction.DisposeAsync();
        }
    }
}
