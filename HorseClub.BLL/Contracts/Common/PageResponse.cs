namespace HorseClub.BLL.Contracts;

public sealed record PageResponse<T>(List<T> Items, int Page, int PageSize, int Total);
