namespace HorseClub.BLL.Common;

public sealed record FileDownload(Stream Content, string ContentType, string FileName);
