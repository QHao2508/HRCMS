namespace HorseClub.DAL.Abstractions;

public interface IFileRepository
{
    Task<Attachment?> GetHorsePhotoAsync(Guid registrationId);
    Task<List<Attachment>> ListAttachmentsAsync(Guid registrationId);
    Task<int> CountAttachmentsAsync(Guid registrationId);
    Task<Attachment?> FindAttachmentAsync(Guid registrationId, Guid id);
    void AddAttachment(Attachment attachment);
    ValueTask<Incident?> FindIncidentAsync(Guid id);
    Task<List<IncidentPhoto>> ListPhotosAsync(Guid incidentId);
    Task<int> CountPhotosAsync(Guid incidentId);
    Task<IncidentPhoto?> FindPhotoAsync(Guid incidentId, Guid id);
    void AddPhoto(IncidentPhoto photo);
    Task<bool> IsStorageReferencedAsync(string storageName);
}
