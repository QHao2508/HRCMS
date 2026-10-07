namespace HorseClub.DAL.Entities;

public sealed class IncidentPhoto : Entity
{
    public Guid IncidentId { get; set; }
    public Guid UploadedBy { get; set; }
    public string FileName { get; set; } = "";
    public string StorageName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
}
