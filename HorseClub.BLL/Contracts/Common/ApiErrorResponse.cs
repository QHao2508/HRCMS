namespace HorseClub.BLL.Contracts;

public sealed record ApiErrorResponse(string Type, string Title, int Status, string Detail, Guid? ReferenceId, string TraceId);
