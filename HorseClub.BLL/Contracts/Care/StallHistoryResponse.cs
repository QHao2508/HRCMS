namespace HorseClub.BLL.Contracts;

public sealed record StallHistoryResponse(Guid Id, Guid HorseId, string HorseName, DateTimeOffset StartedAt, DateTimeOffset? EndedAt);
