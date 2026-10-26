namespace HorseClub.BLL.Contracts;

/// <summary>Lỗi API thống nhất. Client dịch theo MessageKey + Args; Detail tiếng Anh chỉ để log/debug.</summary>
public sealed record ApiErrorResponse(string Type, string Title, int Status, string Detail, Guid? ReferenceId, string TraceId, string MessageKey, string[] Args);
