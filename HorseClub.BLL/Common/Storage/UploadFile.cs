namespace HorseClub.BLL.Common;

public sealed record UploadFile(string Name, string FileName, long Length, Func<Stream> OpenReadStream);
