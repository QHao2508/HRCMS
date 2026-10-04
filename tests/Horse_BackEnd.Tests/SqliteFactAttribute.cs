using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class SqliteFactAttribute : FactAttribute
{
    public SqliteFactAttribute()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HRCMS_TEST_SQLSERVER"))) Skip = "SQLite recovery drill runs in the SQLite test job.";
    }
}
