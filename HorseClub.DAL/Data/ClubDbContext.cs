using Horse_BackEnd.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Horse_BackEnd.Data;

public class ClubDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailChallenge> Challenges => Set<EmailChallenge>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<HorseRegistration> Registrations => Set<HorseRegistration>();
    public DbSet<Horse> Horses => Set<Horse>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<StaffAssignment> Assignments => Set<StaffAssignment>();
    public DbSet<TrainingTemplate> Templates => Set<TrainingTemplate>();
    public DbSet<TrainingPlan> Plans => Set<TrainingPlan>();
    public DbSet<TrainingSession> Sessions => Set<TrainingSession>();
    public DbSet<SessionResult> Results => Set<SessionResult>();
    public DbSet<TrainerEvaluation> Evaluations => Set<TrainerEvaluation>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Injury> Injuries => Set<Injury>();
    public DbSet<MedicalRestriction> Restrictions => Set<MedicalRestriction>();
    public DbSet<TreatmentPlan> Treatments => Set<TreatmentPlan>();
    public DbSet<MedicalFollowUp> FollowUps => Set<MedicalFollowUp>();
    public DbSet<Stable> Stables => Set<Stable>();
    public DbSet<Stall> Stalls => Set<Stall>();
    public DbSet<StallOccupancy> Occupancies => Set<StallOccupancy>();
    public DbSet<CareTask> CareTasks => Set<CareTask>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<PreventiveCare> PreventiveCare => Set<PreventiveCare>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ReplenishmentRequest> Replenishments => Set<ReplenishmentRequest>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<IncidentPhoto> IncidentPhotos => Set<IncidentPhoto>();
    public DbSet<TrainingRevision> TrainingRevisions => Set<TrainingRevision>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditEvent> Audit => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder b)
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
                // UTC ticks allow SQLite to filter/sort timestamps without client evaluation.
                if (p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                    p.SetValueConverter(new ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero)));
            }
        }
        b.Entity<User>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<User>().Property(x => x.UserName).HasMaxLength(80);
        b.Entity<TrainingRevision>().Property(x => x.Snapshot).HasMaxLength(20000);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
            if (entry.State == EntityState.Modified) entry.Entity.Version++;
        return base.SaveChangesAsync(cancellationToken);
    }
}
