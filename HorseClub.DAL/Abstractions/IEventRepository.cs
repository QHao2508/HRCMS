namespace HorseClub.DAL.Abstractions;

public interface IEventRepository
{
    void AddRevision(TrainingRevision revision);
    void AddAudit(AuditEvent audit);
    void AddNotification(Notification notification);
    Task<List<Guid>> GetManagersAsync();
    Task<List<Guid>> GetHorseStaffAsync(Guid horseId, Role[] roles);
    Task<List<Guid>> GetHorseAudienceAsync(Guid horseId);
    Task<List<Guid>> GetRoleAudienceAsync(Role[] roles);
    void AddDataSignal(Guid recipientId);
}
