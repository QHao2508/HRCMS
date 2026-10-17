using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record AttachmentCreatedResponse(
    Guid Id,
    string FileName,
    AttachmentType Type,
    long Length);
