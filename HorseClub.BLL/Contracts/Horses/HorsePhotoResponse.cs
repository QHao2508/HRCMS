namespace HorseClub.BLL.Contracts;

/// <summary>Metadata for a private blob; ContentUrl is served through the scoped, authenticated API.</summary>
public sealed record HorsePhotoResponse(Guid AttachmentId, string FileName, string StorageName, string ContentType, long Length, string ContentUrl);
