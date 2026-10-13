using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.ReferenceImport;

public sealed record ImportResult(int Inserted, int Unchanged, int Total);
public static class ReferenceImporter
{
    public static List<PublicReferenceRecord> Parse(string json)
    {
        using var document = JsonDocument.Parse(json); var root = document.RootElement;
        if (root.GetProperty("importIntoOperationalTables").GetBoolean()) throw new InvalidOperationException("Operational imports are not supported.");
        var date = DateOnly.ParseExact(root.GetProperty("collectedOn").GetString()!, "yyyy-MM-dd");
        var sources = root.GetProperty("sources"); var rows = new List<PublicReferenceRecord>();
        void Add(JsonElement payload, string kind, string name, string identity)
        {
            var sourceKey = payload.GetProperty("source").GetString()!; var source = sources.GetProperty(sourceKey);
            var url = source.GetProperty("url").GetString()!;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https") throw new InvalidOperationException("Invalid provenance URL.");
            var payloadJson = payload.GetRawText(); var metadata = source.GetRawText();
            rows.Add(new PublicReferenceRecord {
                ExternalKey = $"{sourceKey}/{kind}/{identity}/{date:yyyy-MM-dd}", Kind = kind, DisplayName = name,
                SourceUrl = url, CollectedOn = date,
                SourceAsOf = source.TryGetProperty("snapshotAsOf", out var asOf) ? DateOnly.ParseExact(asOf.GetString()!, "yyyy-MM-dd") : null,
                PayloadJson = payloadJson, SourceMetadataJson = metadata,
                ContentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson + "\n" + metadata)))
            });
        }
        foreach (var x in root.GetProperty("veterinaryObservations").EnumerateArray())
            Add(x, "VeterinaryObservation", x.GetProperty("horseName").GetString()!, x.GetProperty("horseCode").GetString() + "/" + x.GetProperty("observedDate").GetString());
        foreach (var x in root.GetProperty("productCatalog").EnumerateArray())
        {
            foreach (var unknown in new[] { "stock", "minimumStock", "priceVnd" })
                if (x.GetProperty(unknown).ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Unverified stock/price cannot be imported.");
            Add(x, "Product", x.GetProperty("name").GetString()!, x.GetProperty("name").GetString()!);
        }
        foreach (var x in root.GetProperty("facilities").EnumerateArray())
            Add(x, "Facility", x.GetProperty("name").GetString()!, x.GetProperty("name").GetString()!);
        if (rows.Count == 0 || rows.Select(x => x.ExternalKey).Distinct().Count() != rows.Count
            || rows.Any(x => x.ExternalKey.Length > 300 || x.PayloadJson.Length > 4000 || x.SourceMetadataJson.Length > 4000))
            throw new InvalidOperationException("Empty, duplicated or oversized reference data.");
        return rows;
    }

    public static async Task<ImportResult> Import(ClubDbContext db, string json)
    {
        var rows = Parse(json); var inserted = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        foreach (var row in rows)
        {
            var existing = await db.PublicReferenceRecords.SingleOrDefaultAsync(x => x.ExternalKey == row.ExternalKey);
            if (existing is not null)
            {
                if (existing.ContentSha256 != row.ContentSha256) throw new InvalidOperationException("Reference snapshot differs; use a new collection date instead of overwriting evidence.");
                continue;
            }
            db.PublicReferenceRecords.Add(row); inserted++;
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return new(inserted, rows.Count - inserted, rows.Count);
    }
}
