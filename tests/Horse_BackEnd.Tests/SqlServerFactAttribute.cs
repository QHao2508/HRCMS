using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HRCMS_TEST_SQLSERVER")))
            Skip = "Set HRCMS_TEST_SQLSERVER to run SQL Server replica/concurrency tests.";
    }
}
