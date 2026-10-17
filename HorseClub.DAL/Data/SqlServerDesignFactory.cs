using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HorseClub.DAL.Data;

public sealed class SqlServerDesignFactory : IDesignTimeDbContextFactory<SqlServerClubDbContext>
{
    /// <summary>
    /// Tạo DbContext design-time cho dotnet-ef từ cấu hình; chỉ dùng để tạo/áp dụng migration SQL Server.
    /// </summary>
    /// <param name="args">Giá trị kiểu string[] dùng trong CreateDbContext.</param>
    public SqlServerClubDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerClubDbContext>()
        .UseSqlServer("Server=localhost;Database=HorseClub;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
