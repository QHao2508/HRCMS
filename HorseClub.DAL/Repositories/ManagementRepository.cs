using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
namespace HorseClub.DAL.Repositories;
public sealed class ManagementRepository(ClubDbContext db):IManagementRepository {
 public Task<WebsiteSettings?> GetWebsite()=>db.WebsiteSettings.SingleOrDefaultAsync(x=>x.Id==WebsiteSettings.PublicId);
 public void AddWebsite(WebsiteSettings settings)=>db.WebsiteSettings.Add(settings);
 public async Task<DataPage<AuditReadModel>> Audit(Guid? referenceId,string? search,DateTimeOffset? from,DateTimeOffset? to,int page,int size){
  var query=from a in db.Audit.AsNoTracking() join u in db.Users on a.ActorId equals u.Id into actors from u in actors.DefaultIfEmpty() select new {Audit=a,ActorName=u==null?"":u.FirstName+" "+u.LastName};
  if(referenceId.HasValue)query=query.Where(x=>x.Audit.ReferenceId==referenceId);
  if(!string.IsNullOrWhiteSpace(search))query=query.Where(x=>x.ActorName.Contains(search));
  if(from.HasValue)query=query.Where(x=>x.Audit.CreatedAt>=from);if(to.HasValue)query=query.Where(x=>x.Audit.CreatedAt<to);
  var total=await query.CountAsync();return new(await query.OrderByDescending(x=>x.Audit.CreatedAt).ThenByDescending(x=>x.Audit.Id).Skip((page-1)*size).Take(size).Select(x=>new AuditReadModel(x.Audit.Id,x.Audit.CreatedAt,x.Audit.ActorId,x.ActorName,x.Audit.Action,x.Audit.ReferenceId,x.Audit.Detail)).ToListAsync(),total);
 }
}
