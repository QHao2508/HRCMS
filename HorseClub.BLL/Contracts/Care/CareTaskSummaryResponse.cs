using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record CareTaskSummaryResponse(
    Guid Id,
    Guid HorseId,
    Guid GroomId,
    CareType Type,
    DateTimeOffset ScheduledAt,
    CareStatus Status,
    DateTimeOffset? CompletedAt,
    decimal? ApprovedPortionKg,
    decimal? ActualPortionKg,
    string? Instructions,
    string? Notes);
