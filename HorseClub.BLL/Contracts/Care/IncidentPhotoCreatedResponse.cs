namespace HorseClub.BLL.Contracts;

public sealed record IncidentPhotoCreatedResponse(
    Guid Id,
    string FileName,
    long Length);
