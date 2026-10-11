using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HorseClub.DAL.Data.Configurations;

internal static class ClubModelConfiguration
{
    /// <summary>
    /// Định nghĩa khóa, quan hệ restrictive, độ dài/precision, version và chuyển DateTimeOffset sang UTC ticks theo schema hiện có.
    /// </summary>
    /// <param name="b">Giá trị kiểu ModelBuilder dùng trong Configure.</param>
    public static void Configure(ModelBuilder b)
    {
        // Every entity is its own table; the shared CLR base does not represent a database hierarchy.
        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Entity))))
        {
            var entity = b.Entity(type);
            entity.HasBaseType((Type?)null);
            entity.HasKey(nameof(Entity.Id));
            entity.Property(nameof(Entity.Version)).IsConcurrencyToken();
        }
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<RealtimeOutboxMessage>().HasIndex(x => new { x.SourceId, x.SourceVersion }).IsUnique();
        b.Entity<RealtimeOutboxMessage>().HasIndex(x => new { x.SentAt, x.NextAttemptAt, x.LockedUntil });
        b.Entity<User>().HasIndex(x => x.UserName).IsUnique();
        b.Entity<Horse>().HasIndex(x => x.RegistrationId).IsUnique();
        b.Entity<SessionResult>().HasIndex(x => x.SessionId).IsUnique();
        b.Entity<TrainerEvaluation>().HasIndex(x => x.SessionId).IsUnique();
        b.Entity<StaffAssignment>().HasIndex(x => new { x.HorseId, x.Role, x.Active });
        b.Entity<MedicalRestriction>().HasIndex(x => new { x.HorseId, x.Cleared });
        b.Entity<Notification>().HasIndex(x => new { x.RecipientId, x.Read });
        b.Entity<EmailChallenge>().HasIndex(x => new { x.UserId, x.Purpose });
        b.Entity<EmailMessage>().HasIndex(x => new { x.SentAt, x.DiscardedAt, x.NextAttemptAt });
        b.Entity<EmailMessage>().HasOne<EmailChallenge>().WithMany().HasForeignKey(x => x.ChallengeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Notification>().HasIndex(x => new { x.ReferenceId, x.Type });
        b.Entity<HorseRegistration>().HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Horse>().HasOne<HorseRegistration>().WithMany().HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Horse>().HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingPlan>().HasOne<TrainingTemplate>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingPlan>().HasOne<User>().WithMany().HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingSession>().HasOne<TrainingPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingSession>().HasOne<User>().WithMany().HasForeignKey(x => x.RiderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SessionResult>().HasOne<TrainingSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainerEvaluation>().HasOne<TrainingSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Attachment>().HasOne<HorseRegistration>().WithMany().HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<IncidentPhoto>().HasOne<Incident>().WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingRevision>().HasOne<TrainingPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingRevision>().HasOne<TrainingSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Stall>().HasOne<Stable>().WithMany().HasForeignKey(x => x.StableId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StallOccupancy>().HasOne<Stall>().WithMany().HasForeignKey(x => x.StallId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReplenishmentRequest>().HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        foreach (var t in b.Model.GetEntityTypes().ToArray())
        {
            if (t.FindProperty("HorseId") is not null)
                b.Entity(t.ClrType).HasOne(typeof(Horse)).WithMany().HasForeignKey("HorseId").OnDelete(DeleteBehavior.Restrict);
            if (t.FindProperty("MedicalRecordId") is not null)
                b.Entity(t.ClrType).HasOne(typeof(MedicalRecord)).WithMany().HasForeignKey("MedicalRecordId").OnDelete(DeleteBehavior.Restrict);
            foreach (var p in t.GetProperties())
            {
                if (p.ClrType == typeof(string)) p.SetMaxLength(4000);
                if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)) { p.SetPrecision(18); p.SetScale(3); }
                // Preserve the UTC-tick storage contract used by the existing SQL Server migrations.
                if (p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                    p.SetValueConverter(new ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero)));
            }
        }
        b.Entity<User>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<User>().Property(x => x.UserName).HasMaxLength(80);
        b.Entity<TrainingRevision>().Property(x => x.Snapshot).HasMaxLength(20000);
        b.Entity<WebsiteSettings>().Property(x=>x.Id).ValueGeneratedNever();
        b.Entity<WebsiteSettings>().Property(x=>x.ContentJson).HasMaxLength(10000);
        ReadPerformanceConfiguration.Configure(b);
    }
}
