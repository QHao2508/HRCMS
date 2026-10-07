using Microsoft.Data.SqlClient;

namespace Horse_BackEnd.Data;

public static class DatabaseFailures
{
    // SQL Server rolls the deadlock victim transaction back. EF may wrap the
    // SqlException in InvalidOperationException or DbUpdateException.
    public static bool IsDeadlock(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(x => x.Number == 1205)) return true;
        return false;
    }
}
