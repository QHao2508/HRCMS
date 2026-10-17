namespace HorseClub.BLL.Contracts;

public sealed record IncidentPhotoResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Length);
