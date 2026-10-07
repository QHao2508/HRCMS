using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Data;

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

    /// <summary>
    /// Áp dụng cấu hình bảng, quan hệ, kiểu dữ liệu, ràng buộc và index cho toàn bộ model SQL Server.
    /// </summary>
    /// <param name="model">Giá trị kiểu ModelBuilder dùng trong OnModelCreating.</param>
    protected override void OnModelCreating(ModelBuilder model) => Configurations.ClubModelConfiguration.Configure(model);

    /// <summary>
    /// Tăng Version cho entity bị sửa trước khi lưu để phát hiện cập nhật đồng thời bằng optimistic concurrency.
    /// </summary>
    /// <param name="cancellationToken">Giá trị kiểu CancellationToken dùng trong SaveChangesAsync.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
            if (entry.State == EntityState.Modified) entry.Entity.Version++;
        return base.SaveChangesAsync(cancellationToken);
    }
}
