using Microsoft.Data.SqlClient;

namespace HorseClub.DAL.Data;

public static class DatabaseFailures
{
    // SQL Server rolls the deadlock victim transaction back. EF may wrap the
    // SqlException in InvalidOperationException or DbUpdateException.
    /// <summary>
    /// Nhận diện mã deadlock SQL Server trong chuỗi exception để API trả xung đột có thể tải lại.
    /// </summary>
    /// <param name="error">Lỗi từ HTTP/network/ghi dữ liệu cần chuẩn hóa hoặc trình bày an toàn.</param>
    public static bool IsDeadlock(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(x => x.Number == 1205)) return true;
        return false;
    }
}
