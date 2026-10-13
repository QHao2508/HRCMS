using System.Text.Json;
using HorseClub.ReferenceImport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class PublicReferenceImportTests
{
    private static string Dataset()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docs/data/flow34-public-reference-2026-10-13.json"))) directory = directory.Parent;
        return File.ReadAllText(Path.Combine(directory!.FullName, "docs/data/flow34-public-reference-2026-10-13.json"));
    }

    [Fact]
    public async Task ImportPreservesUnknownsAndProvenanceAndSecondRunDoesNotDuplicateOrChangeClubRecords()
    {
        await using var factory = new ClubFactory(); using var client = await factory.Client(); var json = Dataset();
        await WorkerTests.Read(factory, async db => {
            var users = await db.Users.CountAsync();
            Assert.Equal(new ImportResult(13, 0, 13), await ReferenceImporter.Import(db, json));
            Assert.Equal(new ImportResult(0, 13, 13), await ReferenceImporter.Import(db, json));
            Assert.Equal(13, await db.PublicReferenceRecords.CountAsync());
            Assert.Equal(users, await db.Users.CountAsync());
            Assert.Empty(await db.Horses.ToListAsync()); Assert.Empty(await db.MedicalRecords.ToListAsync());
            Assert.Empty(await db.Inventory.ToListAsync()); Assert.Empty(await db.Occupancies.ToListAsync());
            var product = await db.PublicReferenceRecords.FirstAsync(x => x.Kind == "Product");
            using var payload = JsonDocument.Parse(product.PayloadJson);
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("stock").ValueKind);
            Assert.StartsWith("https://", product.SourceUrl); Assert.Equal(64, product.ContentSha256.Length);
            var medical = await db.PublicReferenceRecords.FirstAsync(x => x.Kind == "VeterinaryObservation");
            Assert.Equal(new DateOnly(2026, 10, 10), medical.SourceAsOf);
        });
    }

    [Fact]
    public async Task ChangedSnapshotIsRejectedAndTransactionDoesNotPartiallyInsert()
    {
        await using var factory = new ClubFactory(); using var client = await factory.Client(); var json = Dataset();
        await WorkerTests.Read(factory, async db => {
            await ReferenceImporter.Import(db, json);
            var changed = json.Replace("Ăn kém.", "Changed observation");
            await Assert.ThrowsAsync<InvalidOperationException>(() => ReferenceImporter.Import(db, changed));
            Assert.Equal(13, await db.PublicReferenceRecords.CountAsync());
            var stored = await db.PublicReferenceRecords.FirstAsync(x => x.DisplayName == "SUPER HONG KONG");
            Assert.DoesNotContain("Changed observation", stored.PayloadJson);
        });
    }

    [Fact]
    public void UnverifiedStockCannotBePretendedToBeReal()
    {
        Assert.Throws<InvalidOperationException>(() => ReferenceImporter.Parse(Dataset().Replace("\"stock\": null", "\"stock\": 10")));
    }
}
