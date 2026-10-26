using System.Data;
using System.Security.Claims;
using HorseClub.BLL.Abstractions.Services;
using HorseClub.BLL.Common;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Inventory;
using HorseClub.BLL.Training;
using HorseClub.DAL;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HorseClub.OperationalData;

public sealed record DemoTrainer(string UserName, string ExpectedEligibility, List<ExpertiseInput> Specialties);
public sealed record DemoReceipt(string Name, string Unit, decimal Quantity);
public sealed record DemoOperationsManifest(string BatchKey, string TemplateSystemKey, List<DemoTrainer> Trainers, List<DemoReceipt> Receipts);
public sealed record DemoEligibility(string UserName, string Eligibility);
public sealed record DemoOperationsSummary(int UpdatedProfiles, int ReceivedItems, List<DemoEligibility> Candidates, bool Applied);

/// <summary>Explicit demo data only. Uses business services and one transaction, including preview rollback.</summary>
public static class DemoOperationsImporter
{
    public static async Task<DemoOperationsSummary> Import(SqlServerClubDbContext db, User manager, DemoOperationsManifest manifest, bool apply)
    {
        static bool Text(string? text) => !string.IsNullOrWhiteSpace(text) && text.Trim() == text && text.Length <= 200;
        if (!Text(manifest.BatchKey) || manifest.BatchKey.Length > 80 || !Text(manifest.TemplateSystemKey)
            || manifest.Trainers is null || manifest.Receipts is null || manifest.Trainers.Count is < 1 or > 20 || manifest.Receipts.Count > 100
            || manifest.Trainers.Any(t => t is null || !Text(t.UserName) || t.ExpectedEligibility is not ("Eligible" or "MissingSpecialty" or "BelowMinimumLevel")
                || t.Specialties is null || t.Specialties.Count > 7 || t.Specialties.Any(s => s is null || !Enum.IsDefined(s.TrainingType) || s.Level is < 1 or > 3)
                || t.Specialties.Select(s => s.TrainingType).Distinct().Count() != t.Specialties.Count)
            || manifest.Receipts.Any(r => r is null || !Text(r.Name) || !Text(r.Unit) || r.Quantity is <= 0 or > 1000000 || decimal.Round(r.Quantity, 3) != r.Quantity)
            || manifest.Trainers.Select(t => t.UserName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Trainers.Count
            || manifest.Receipts.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Receipts.Count)
            throw new InvalidOperationException("Invalid or duplicate demo operations entries.");

        // The CLI verifies the manager's existing password before calling this privileged data operation.
        var services = new ServiceCollection();
        services.AddSingleton<ClubDbContext>(db);
        services.AddSingleton<ICurrentIdentity>(new ImportIdentity(manager));
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<BusinessOptions>();
        services.AddClubDataAccess();
        services.AddScoped<CurrentUser>(); services.AddScoped<ClubAccess>(); services.AddScoped<ClubEvents>();
        services.AddScoped<PageReader>(); services.AddScoped<ClubCalendar>();
        services.AddScoped<ITrainingTemplateService, TrainingTemplateService>();
        services.AddScoped<ITrainingSchedulingService, TrainingSchedulingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var profiles = scope.ServiceProvider.GetRequiredService<ITrainingSchedulingService>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var templates = scope.ServiceProvider.GetRequiredService<ITrainingTemplateService>();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var template = await db.Templates.AsNoTracking().SingleAsync(t => t.SystemKey == manifest.TemplateSystemKey && t.IsSystemDefault && !t.Archived);
            var changed = 0; var received = 0; var candidates = new List<DemoEligibility>();
            foreach (var row in manifest.Trainers)
            {
                var trainer = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == row.UserName && u.Role == Role.Trainer && u.Active && u.EmailVerified);
                var profile = await profiles.GetProfile(trainer.Id);
                if (!profile.Specialties.OrderBy(s => s.TrainingType).SequenceEqual(row.Specialties.OrderBy(s => s.TrainingType)))
                {
                    // Preserve hours/time off; the service refuses changes that invalidate assigned templates or booked sessions.
                    await profiles.UpdateProfile(trainer.Id, new(row.Specialties, profile.WorkingHours, profile.TimeOff));
                    changed++;
                }
                var candidate = (await templates.Candidates(template.Id)).Single(c => c.Id == trainer.Id);
                if (candidate.Eligibility != row.ExpectedEligibility)
                    throw new InvalidOperationException($"Unexpected eligibility for {row.UserName}; transaction rolled back.");
                candidates.Add(new(row.UserName, candidate.Eligibility));
            }
            foreach (var row in manifest.Receipts)
            {
                var item = await db.Inventory.SingleAsync(i => i.Name == row.Name && !i.Archived);
                if (item.Unit != row.Unit) throw new InvalidOperationException("Receipt unit differs from the existing inventory unit.");
                var marker = $"{manifest.BatchKey}: {row.Name}";
                var requests = await db.Replenishments.Where(r => r.ItemId == item.Id && r.Notes == marker).ToListAsync();
                if (requests.Count > 1) throw new InvalidOperationException("Duplicate batch receipts require review.");
                var request = requests.SingleOrDefault();
                if (request is not null && (request.Quantity != row.Quantity || request.Status == ReplenishmentStatus.Rejected))
                    throw new InvalidOperationException("Existing batch receipt differs from the manifest.");
                if (request?.Status == ReplenishmentStatus.Received) continue;
                request ??= await inventory.RequestReplenishment(new(item.Id, row.Quantity, marker));
                if (request.Status == ReplenishmentStatus.Pending)
                    await inventory.ReviewReplenishment(request.Id, new(true, "Dữ liệu minh họa P1; không phải chứng từ mua hàng thực tế."));
                await inventory.ReceiveReplenishment(request.Id, new(request.Quantity - request.ReceivedQuantity,
                    $"{manifest.BatchKey}: Nhập tồn minh họa; không phải hàng nhận thực tế."));
                received++;
            }
            if (apply) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            return new(changed, received, candidates, apply);
        }
        catch
        {
            await transaction.RollbackAsync(); db.ChangeTracker.Clear(); throw;
        }
    }

    private sealed class ImportIdentity(User user) : ICurrentIdentity
    {
        public ClaimsPrincipal Principal { get; } = new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("stamp", user.SecurityStamp)
        ], "DemoOperations"));
    }
}
