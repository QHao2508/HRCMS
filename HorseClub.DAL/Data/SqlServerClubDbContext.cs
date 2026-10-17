using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Data;

public sealed class SqlServerClubDbContext(DbContextOptions<SqlServerClubDbContext> options) : ClubDbContext(options);
