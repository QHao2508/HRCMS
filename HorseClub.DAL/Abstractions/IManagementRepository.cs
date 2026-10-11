namespace HorseClub.DAL.Abstractions;
public sealed record AuditReadModel(Guid Id,DateTimeOffset CreatedAt,Guid ActorId,string ActorName,AuditAction Action,Guid ReferenceId,string Detail);
public interface IManagementRepository {
 Task<WebsiteSettings?> GetWebsite();
 void AddWebsite(WebsiteSettings settings);
 Task<DataPage<AuditReadModel>> Audit(Guid? referenceId,string? search,DateTimeOffset? from,DateTimeOffset? to,int page,int size);
}
