using EzBias.Domain.Exceptions;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
        catch (DbUpdateException ex) when (IsFandomWriteConflict(ex))
        {
            throw new FandomWriteConflictException("A conflicting fandom was created concurrently.", ex);
        }
    }

    private static bool IsFandomWriteConflict(DbUpdateException exception)
    {
        var postgresException = exception.InnerException as PostgresException;
        return postgresException?.ConstraintName is "ux_fandoms_normalized_name" or "PK_fandoms";
    }
}
