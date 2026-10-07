using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record AttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Length,
    AttachmentType Type,
    string? CertificateNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate);
