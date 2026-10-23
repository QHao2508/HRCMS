using System.Data;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.OperationalData;

public sealed record CatalogItem(string Name, string Category, string Unit, string SourceUrl);
public sealed record StableLayout(string Name, List<string> Stalls);
public sealed record InventoryRename(Guid Id, string ExpectedName, string Name, string Category);
public sealed record OperationalManifest(List<CatalogItem> Items, List<StableLayout> Stables, List<InventoryRename> Renames);
public sealed record ImportSummary(int NewItems, int NewStables, int NewStalls, int RenamedItems, bool Applied);

/// <summary>Explicit catalogue/layout import, separate from schema migrations and runtime startup.</summary>
public static class OperationalDataImporter
{
    public static async Task<ImportSummary> Import(SqlServerClubDbContext db, OperationalManifest manifest, bool apply)
    {
        static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim() == value && value.Length <= 200;
        if (manifest.Items is null || manifest.Stables is null || manifest.Renames is null
            || manifest.Items.Count > 100 || manifest.Stables.Count > 20 || manifest.Renames.Count > 100
            || manifest.Items.Any(x => !Text(x.Name) || !Text(x.Category) || !Text(x.Unit) || !Uri.TryCreate(x.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            || manifest.Stables.Any(x => !Text(x.Name) || x.Stalls is null || x.Stalls.Count is < 1 or > 100 || x.Stalls.Any(s => !Text(s)) || x.Stalls.Distinct(StringComparer.OrdinalIgnoreCase).Count() != x.Stalls.Count)
            || manifest.Renames.Any(x => x.Id == Guid.Empty || !Text(x.ExpectedName) || !Text(x.Name) || !Text(x.Category))
            || manifest.Items.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Items.Count
            || manifest.Stables.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Stables.Count
            || manifest.Renames.Select(x => x.Id).Distinct().Count() != manifest.Renames.Count)
            throw new InvalidOperationException("Invalid or duplicate catalogue/layout entries.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var inventory = await db.Inventory.ToListAsync();
        var stables = await db.Stables.ToListAsync(); var stalls = await db.Stalls.ToListAsync();
        var itemsAdded = 0; var stablesAdded = 0; var stallsAdded = 0; var renamed = 0;
        foreach (var rename in manifest.Renames)
        {
            var row = inventory.SingleOrDefault(x => x.Id == rename.Id) ?? throw new InvalidOperationException("Rename target does not exist.");
            if (row.Name == rename.Name && row.Category == rename.Category) continue;
            if (row.Name != rename.ExpectedName || row.Archived || inventory.Any(x => x.Id != row.Id && x.Name.Equals(rename.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Rename precondition failed; review the inventory mapping.");
            // Preserve ID, stock, unit and all movement/replenishment history. Never reinterpret units.
            row.Name = rename.Name; row.Category = rename.Category; renamed++;
        }
        foreach (var item in manifest.Items)
        {
            var matches = inventory.Where(x => x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1 || matches.Any(x => x.Archived || x.Category != item.Category || x.Unit != item.Unit))
                throw new InvalidOperationException("Existing item is ambiguous, archived or uses a different category/unit.");
            if (matches.Count == 1) continue;
            var row = new InventoryItem { Name = item.Name, Category = item.Category, Unit = item.Unit, Stock = 0, MinimumStock = 0 };
            inventory.Add(row); db.Inventory.Add(row); itemsAdded++;
        }
        foreach (var layout in manifest.Stables)
        {
            var matches = stables.Where(x => x.Name.Equals(layout.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1) throw new InvalidOperationException("Stable name is ambiguous.");
            var stable = matches.SingleOrDefault();
            if (stable is null) { stable = new Stable { Name = layout.Name }; stables.Add(stable); db.Stables.Add(stable); stablesAdded++; }
            foreach (var name in layout.Stalls)
            {
                var existing = stalls.Where(x => x.StableId == stable.Id && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
                if (existing.Count > 1) throw new InvalidOperationException("Stall name is ambiguous.");
                if (existing.Count == 1) continue;
                // Default NeedsCleaning: do not invent a cleaning inspection or occupancy history.
                var stall = new Stall { StableId = stable.Id, Name = name }; stalls.Add(stall); db.Stalls.Add(stall); stallsAdded++;
            }
        }
        if (apply) { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        else { await transaction.RollbackAsync(); db.ChangeTracker.Clear(); }
        return new(itemsAdded, stablesAdded, stallsAdded, renamed, apply);
    }
}
