using System.Text.Json;
using HorseClub.BLL.Common;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using HorseClub.OperationalData;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class P1DemoDataTests
{
    private static async Task<T> Manifest<T>(string name) => JsonSerializer.Deserialize<T>(
        await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data", name)), ClubFactory.Json)!;

    [Fact]
    public async Task RealManifestsPreviewRollbackAndApplyOnceWithThreeEligibilityCasesAndReceiptHistory()
    {
        await using var factory = new ClubFactory(); _ = factory.Services;
        var catalog = await Manifest<OperationalManifest>("operational-catalog.proposed.json");
        var operations = await Manifest<DemoOperationsManifest>("p1-demo-operations.json");
        await WorkerTests.Read(factory, async context =>
        {
            var db = (SqlServerClubDbContext)context;
            var manager = await db.Users.SingleAsync(u => u.Role == Role.ClubManager);
            foreach (var row in operations.Trainers)
            {
                var trainer = new User { UserName = row.UserName, Email = row.UserName + "@example.test", Role = Role.Trainer, EmailVerified = true };
                db.Users.Add(trainer);
                db.TrainerExpertise.AddRange(Enum.GetValues<TrainingType>().Select(t => new TrainerExpertise { StaffId = trainer.Id, TrainingType = t, Level = 3 }));
                db.StaffWorkingHours.Add(new() { StaffId = trainer.Id, DayOfWeek = DayOfWeek.Monday, StartMinute = 360, EndMinute = 600 });
            }
            await db.SaveChangesAsync();
            await OperationalDataImporter.Import(db, catalog, true);
            Assert.Equal(17, await db.Inventory.CountAsync()); Assert.Equal(14, await db.Stalls.CountAsync());
            var originalSkills = await db.TrainerExpertise.CountAsync();
            var originalAudit = await db.Audit.CountAsync();
            var preview = await DemoOperationsImporter.Import(db, manager, operations, false);
            Assert.False(preview.Applied); Assert.Equal(3, preview.UpdatedProfiles); Assert.Equal(17, preview.ReceivedItems);
            Assert.Equal(originalSkills, await db.TrainerExpertise.CountAsync()); Assert.Equal(originalAudit, await db.Audit.CountAsync());
            Assert.Empty(await db.StockMovements.ToListAsync()); Assert.Empty(await db.Replenishments.ToListAsync());
            Assert.All(await db.Inventory.ToListAsync(), i => Assert.Equal(0, i.Stock));

            var applied = await DemoOperationsImporter.Import(db, manager, operations, true);
            Assert.Equal(operations.Trainers.Select(t => t.ExpectedEligibility), applied.Candidates.Select(c => c.Eligibility));
            Assert.Equal(17, applied.ReceivedItems); Assert.Equal(17, await db.StockMovements.CountAsync());
            Assert.All(await db.Replenishments.ToListAsync(), r => { Assert.Equal(ReplenishmentStatus.Received, r.Status); Assert.Equal(r.Quantity, r.ReceivedQuantity); Assert.Equal(manager.Id, r.ReceivedBy); });
            foreach (var item in await db.Inventory.ToListAsync())
            {
                Assert.Equal(operations.Receipts.Single(r => r.Name == item.Name).Quantity, item.Stock);
                Assert.Equal(item.Stock, await db.StockMovements.Where(m => m.ItemId == item.Id).SumAsync(m => m.Quantity));
            }
            var auditCount = await db.Audit.CountAsync();
            var repeated = await DemoOperationsImporter.Import(db, manager, operations, true);
            Assert.Equal(0, repeated.UpdatedProfiles); Assert.Equal(0, repeated.ReceivedItems);
            Assert.Equal(auditCount, await db.Audit.CountAsync()); Assert.Equal(17, await db.StockMovements.CountAsync());
            Assert.Equal(3, await db.StaffWorkingHours.CountAsync());
            Assert.All(await db.StaffWorkingHours.ToListAsync(), h => { Assert.Equal(360, h.StartMinute); Assert.Equal(600, h.EndMinute); });
            Assert.All(await db.Stalls.ToListAsync(), s => Assert.Equal(CleaningStatus.NeedsCleaning, s.CleaningStatus));
        });
    }

    [Fact]
    public async Task AssignedTemplateConflictRejectsProfileChangesAndRollsBackTheWholeBatch()
    {
        await using var factory = new ClubFactory(); _ = factory.Services;
        var operations = await Manifest<DemoOperationsManifest>("p1-demo-operations.json");
        await WorkerTests.Read(factory, async context =>
        {
            var db = (SqlServerClubDbContext)context;
            var manager = await db.Users.SingleAsync(u => u.Role == Role.ClubManager);
            foreach (var row in operations.Trainers)
            {
                var trainer = new User { UserName = row.UserName, Email = row.UserName + "@example.test", Role = Role.Trainer, EmailVerified = true };
                db.Users.Add(trainer);
                db.TrainerExpertise.AddRange(Enum.GetValues<TrainingType>().Select(t => new TrainerExpertise { StaffId = trainer.Id, TrainingType = t, Level = 3 }));
                if (row.UserName == "trainer.bao") db.TrainerTemplateAssignments.Add(new() { TrainerId = trainer.Id, AssignedBy = manager.Id, TemplateId = DefaultTrainingTemplates.All[0].Id });
            }
            await db.SaveChangesAsync();
            var error = await Assert.ThrowsAsync<ApiException>(() => DemoOperationsImporter.Import(db, manager, operations with { Receipts = [] }, true));
            Assert.Equal("trainer_specialty_mismatch", error.Code);
            Assert.Equal(21, await db.TrainerExpertise.CountAsync()); // The first profile's changes were also rolled back.
            Assert.All(await db.TrainerExpertise.ToListAsync(), s => Assert.Equal(3, s.Level));
        });
    }
}
