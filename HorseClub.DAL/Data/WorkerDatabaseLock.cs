using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Horse_BackEnd.Data;

public static class WorkerDatabaseLock
{
    // The caller also holds the process gate for single-instance SQLite.
    // SQL Server locks are scoped to this database and released with the transaction.
    public static async Task<bool> TryAcquire(ClubDbContext db, string resource, CancellationToken token)
    {
        if (!db.Database.IsSqlServer()) return true;
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Worker lock requires an active transaction.");
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; SELECT @result;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource"; parameter.DbType = DbType.String; parameter.Value = resource;
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(token));
        if (result == -1) return false; // Another replica is working; try at the next poll.
        if (result < 0) throw new InvalidOperationException($"Worker lock failed ({result}).");
        return true;
    }
}
