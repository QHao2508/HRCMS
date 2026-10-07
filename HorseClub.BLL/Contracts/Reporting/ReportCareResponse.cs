using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record ReportCareResponse(
    Guid Id,
    Guid HorseId,
    CareType Type,
    CareStatus Status,
    DateTimeOffset ScheduledAt,
    decimal? ApprovedPortionKg,
    decimal? ActualPortionKg);
