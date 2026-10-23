using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using HorseClub.OperationalData;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class OperationalDataTests
{
    [Fact]
    public async Task PreviewDoesNotWriteAndApplyIsRepeatableWithoutInventingStockOrOccupancy()
    {
        await using var factory = new ClubFactory(); _ = factory.Services;
        var manifest = new OperationalManifest([new("Cỏ khô Timothy", "Thức ăn thô", "kg", "https://extension.umn.edu/")], [new("Khu A", ["A01", "A02"])], []);
        await WorkerTests.Read(factory, async context =>
        {
            var db = (SqlServerClubDbContext)context;
            Assert.Equal(new ImportSummary(1, 1, 2, 0, false), await OperationalDataImporter.Import(db, manifest, false));
            Assert.Empty(await db.Inventory.ToListAsync()); Assert.Empty(await db.Stalls.ToListAsync());
            Assert.Equal(new ImportSummary(1, 1, 2, 0, true), await OperationalDataImporter.Import(db, manifest, true));
            Assert.Equal(new ImportSummary(0, 0, 0, 0, true), await OperationalDataImporter.Import(db, manifest, true));
            var item = Assert.Single(await db.Inventory.ToListAsync()); Assert.Equal(0, item.Stock); Assert.Equal(0, item.MinimumStock);
            Assert.All(await db.Stalls.ToListAsync(), s => Assert.Equal(CleaningStatus.NeedsCleaning, s.CleaningStatus));
            Assert.Empty(await db.Occupancies.ToListAsync()); Assert.Empty(await db.StockMovements.ToListAsync());
        });
    }

    [Fact]
    public async Task RenamePreservesHistoricalIdsUnitsAndStockAndInvalidMappingRollsBack()
    {
        await using var factory = new ClubFactory(); _ = factory.Services;
        await WorkerTests.Read(factory, async context =>
        {
            var db = (SqlServerClubDbContext)context;
            var item = new InventoryItem { Name = "Vật tư cũ", Category = "Mẫu", Unit = "kg", Stock = 5 };
            var actor = await db.Users.FirstAsync();
            var movement = new StockMovement { ItemId = item.Id, ActorId = actor.Id, Quantity = 5, Reason = "Inventory record" };
            db.Inventory.Add(item); db.StockMovements.Add(movement); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var rename = new InventoryRename(item.Id, item.Name, "Cỏ khô Timothy", "Thức ăn thô");
            var manifest = new OperationalManifest([], [], [rename]);
            await OperationalDataImporter.Import(db, manifest, true); db.ChangeTracker.Clear();
            var stored = await db.Inventory.SingleAsync(); Assert.Equal(item.Id, stored.Id); Assert.Equal(5, stored.Stock); Assert.Equal("kg", stored.Unit);
            Assert.Equal(item.Id, (await db.StockMovements.SingleAsync()).ItemId);
            Assert.Equal(0, (await OperationalDataImporter.Import(db, manifest, true)).RenamedItems);
            var invalid = new OperationalManifest([], [], [rename with { ExpectedName = "Wrong old name", Name = "Another item" }]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => OperationalDataImporter.Import(db, invalid, true));
            db.ChangeTracker.Clear(); Assert.Equal("Cỏ khô Timothy", (await db.Inventory.SingleAsync()).Name);
        });
    }
}
