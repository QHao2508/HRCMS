using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class EventRepository(ClubDbContext db) : IEventRepository
{
    public void AddRevision(TrainingRevision revision) => db.TrainingRevisions.Add(revision);
    public void AddAudit(AuditEvent audit) => db.Audit.Add(audit);
    public void AddNotification(Notification notification) => db.Notifications.Add(notification);
    public Task<List<Guid>> GetManagersAsync() => db.Users.Where(x => x.Active && x.Role == Role.ClubManager).Select(x => x.Id).ToListAsync();
    public Task<List<Guid>> GetHorseStaffAsync(Guid horseId, Role[] roles) => db.Assignments.Where(x => x.HorseId == horseId && x.Active && roles.Contains(x.Role)).Select(x => x.StaffId).Distinct().ToListAsync();
    public Task<List<Guid>> GetRoleAudienceAsync(Role[] roles) => db.Users.Where(x => x.Active && x.EmailVerified && roles.Contains(x.Role)).Select(x => x.Id).ToListAsync();
    public async Task<List<Guid>> GetHorseAudienceAsync(Guid horseId)
    {
        var ids = await db.Horses.Where(x => x.Id == horseId).Select(x => x.OwnerId)
            .Union(db.Assignments.Where(x => x.HorseId == horseId && x.Active).Select(x => x.StaffId))
            .Union(db.Sessions.Where(x => x.HorseId == horseId && x.RiderId != null).Select(x => x.RiderId!.Value))
            .Union(db.Users.Where(x => x.Active && x.Role == Role.ClubManager).Select(x => x.Id)).ToListAsync();
        ids.AddRange(db.Assignments.Local.Where(x => x.HorseId == horseId && x.Active).Select(x => x.StaffId));
        ids.AddRange(db.Sessions.Local.Where(x => x.HorseId == horseId && x.RiderId.HasValue).Select(x => x.RiderId!.Value));
        return ids.Distinct().ToList();
    }
    public void AddDataSignal(Guid recipientId)
        => db.RealtimeOutboxMessages.Add(new RealtimeOutboxMessage { RecipientId = recipientId, SourceId = Guid.NewGuid(), EventType = "DataChanged" });
}
