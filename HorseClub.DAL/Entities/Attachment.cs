namespace HorseClub.DAL.Entities;

public sealed class Attachment : Entity
{
    public Guid RegistrationId { get; set; }
    public Guid UploadedBy { get; set; }
    public string FileName { get; set; } = "";
    public string StorageName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public AttachmentType Type { get; set; }
    public string? CertificateNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
