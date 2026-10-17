using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Data.Configurations;

/// <summary>Composite indexes aligned with scope checks, dashboard filters and paginated histories.</summary>
internal static class ReadPerformanceConfiguration
{
    /// <summary>
    /// Định nghĩa index phục vụ truy vấn danh sách/phạm vi/lịch, giữ truy vấn phổ biến tránh scan không cần thiết.
    /// </summary>
    /// <param name="model">Giá trị kiểu ModelBuilder dùng trong Configure.</param>
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Horse>().HasIndex(x => new { x.OwnerId, x.Archived });
        model.Entity<HorseRegistration>().HasIndex(x => new { x.OwnerId, x.Status, x.CreatedAt });
        model.Entity<StaffAssignment>().HasIndex(x => new { x.StaffId, x.Active, x.HorseId });
        model.Entity<TrainingPlan>().HasIndex(x => new { x.HorseId, x.Status });
        model.Entity<TrainingSession>().HasIndex(x => new { x.HorseId, x.Status, x.ScheduledAt });
        model.Entity<TrainingSession>().HasIndex(x => new { x.RiderId, x.Status, x.ScheduledAt });
        model.Entity<TrainingSession>().HasIndex(x => new { x.PlanId, x.ScheduledAt });
        model.Entity<CareTask>().HasIndex(x => new { x.HorseId, x.ScheduledAt });
        model.Entity<CareTask>().HasIndex(x => new { x.GroomId, x.ScheduledAt });
        model.Entity<Measurement>().HasIndex(x => new { x.HorseId, x.Date });
        model.Entity<Notification>().HasIndex(x => new { x.RecipientId, x.Read, x.CreatedAt });
        model.Entity<StockMovement>().HasIndex(x => new { x.ItemId, x.CreatedAt });
        model.Entity<TrainingRevision>().HasIndex(x => new { x.PlanId, x.CreatedAt });
        model.Entity<AuditEvent>().HasIndex(x => new { x.ReferenceId, x.CreatedAt });
    }
}
