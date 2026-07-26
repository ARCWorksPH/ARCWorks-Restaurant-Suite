using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Roms.Infrastructure.Persistence;

/// <summary>
/// Oracle Connector/NET uses GET_LOCK(name, -1) for the EF migration lock.
/// MariaDB returns NULL for a negative timeout, which Connector/NET then tries
/// to cast to Int64. A finite timeout has the same exclusivity semantics and is
/// supported by both MySQL and MariaDB.
/// </summary>
public sealed class MariaDbMigrationLockInterceptor : DbCommandInterceptor
{
    private const string EfLock = "GET_LOCK('__EFMigrationsLock',-1)";
    private const string MariaDbCompatibleLock = "GET_LOCK('__EFMigrationsLock',60)";

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        RewriteMigrationLock(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        RewriteMigrationLock(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        return NormalizeMigrationLockResult(command, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(NormalizeMigrationLockResult(command, result));
    }

    private static void RewriteMigrationLock(DbCommand command)
    {
        if (command.CommandText.Contains(EfLock, StringComparison.Ordinal))
            command.CommandText = command.CommandText.Replace(EfLock, MariaDbCompatibleLock, StringComparison.Ordinal);
    }

    private static object? NormalizeMigrationLockResult(DbCommand command, object? result)
    {
        if (command.CommandText.Contains("GET_LOCK('__EFMigrationsLock'", StringComparison.Ordinal) && result is int value)
            return (long)value;
        return result;
    }
}
