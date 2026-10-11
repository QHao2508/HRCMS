using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class FileRepository(ClubDbContext db) : IFileRepository
{
    public Task<Attachment?> GetHorsePhotoAsync(Guid registrationId) => db.Attachments.Where(x => x.RegistrationId == registrationId && x.Type == AttachmentType.HorsePhoto).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync();
    public Task<List<Attachment>> ListAttachmentsAsync(Guid registrationId) => db.Attachments.AsNoTracking().Where(x => x.RegistrationId == registrationId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync();
    public Task<int> CountAttachmentsAsync(Guid registrationId) => db.Attachments.CountAsync(x => x.RegistrationId == registrationId);
    public Task<Attachment?> FindAttachmentAsync(Guid registrationId, Guid id) => db.Attachments.SingleOrDefaultAsync(x => x.Id == id && x.RegistrationId == registrationId);
    public void AddAttachment(Attachment attachment) => db.Attachments.Add(attachment);
    public ValueTask<Incident?> FindIncidentAsync(Guid id) => db.Incidents.FindAsync(id);
    public Task<List<IncidentPhoto>> ListPhotosAsync(Guid incidentId) => db.IncidentPhotos.AsNoTracking().Where(x => x.IncidentId == incidentId).ToListAsync();
    public Task<int> CountPhotosAsync(Guid incidentId) => db.IncidentPhotos.CountAsync(x => x.IncidentId == incidentId);
    public Task<IncidentPhoto?> FindPhotoAsync(Guid incidentId, Guid id) => db.IncidentPhotos.SingleOrDefaultAsync(x => x.Id == id && x.IncidentId == incidentId);
    public void AddPhoto(IncidentPhoto photo) => db.IncidentPhotos.Add(photo);
    public async Task<bool> IsStorageReferencedAsync(string storageName)
        => await db.Attachments.AnyAsync(x => x.StorageName == storageName) || await db.IncidentPhotos.AnyAsync(x => x.StorageName == storageName)
            || await db.WebsiteSettings.AnyAsync(x => x.LogoName == storageName || x.HeroName == storageName || x.BackgroundName == storageName);
}
