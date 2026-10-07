using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HorseClub.DAL.Data;

public static class WorkerDatabaseLock
{
    // SQL Server locks are scoped to this database and released with the transaction.
    /// <summary>
    /// Lấy application lock của SQL Server trong transaction để nhiều instance không xử lý cùng lô công việc; trả false khi instance khác đang giữ khóa.
    /// </summary>
    /// <param name="db">Giá trị kiểu ClubDbContext dùng trong TryAcquire.</param>
    /// <param name="resource">Giá trị kiểu string dùng trong TryAcquire.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public static async Task<bool> TryAcquire(ClubDbContext db, string resource, CancellationToken token)
    {
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
