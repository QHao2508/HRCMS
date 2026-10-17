using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class StaffSampleSeedTests
{
    [Fact]
    public async Task FullStaffProfileIsStoredAndRerunDoesNotDuplicateOrQueueEmail()
    {
        await using var factory = new ClubFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HorseClub.DAL.Data.ClubDbContext>();
        var seeder = new DemoAccountSeeder(new HorseClub.DAL.Repositories.AuthRepository(db), new HorseClub.DAL.Data.EfUnitOfWork(db));
        var account = new DemoAccountSeeder.Account("demo.vet01@example.test", "demo.vet01", Role.Veterinarian,
            "Nguyễn Minh", "Anh", "0900000101", "Khu mẫu A, TP. Hồ Chí Minh");
        Assert.Equal(1, await seeder.Seed(new[] {account}, ClubFactory.Password, new SecurityOptions()));
        Assert.Equal(0, await seeder.Seed(new[] {account}, ClubFactory.Password, new SecurityOptions()));
        var user = await db.Users.SingleAsync(u => u.UserName == account.UserName);
        Assert.Equal(account.FirstName, user.FirstName); Assert.Equal(account.LastName, user.LastName);
        Assert.Equal(account.Phone, user.Phone); Assert.Equal(account.Address, user.Address);
        Assert.True(user.Active); Assert.True(user.EmailVerified); Assert.NotEqual(ClubFactory.Password, user.PasswordHash);
        Assert.Empty(await db.EmailMessages.ToListAsync());
    }
}
