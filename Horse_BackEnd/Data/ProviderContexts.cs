using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Horse_BackEnd.Data;

public sealed class SqliteClubDbContext(DbContextOptions<SqliteClubDbContext> options) : ClubDbContext(options);
public sealed class SqlServerClubDbContext(DbContextOptions<SqlServerClubDbContext> options) : ClubDbContext(options);

public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteClubDbContext>
{
    public SqliteClubDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteClubDbContext>()
        .UseSqlite("Data Source=horseclub-design.db").Options);
}
public sealed class SqlServerDesignFactory : IDesignTimeDbContextFactory<SqlServerClubDbContext>
{
    public SqlServerClubDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerClubDbContext>()
        .UseSqlServer("Server=localhost;Database=HorseClub;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
