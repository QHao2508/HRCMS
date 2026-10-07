using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record ReportTrainingResponse(
    Guid Id,
    Guid HorseId,
    DateTimeOffset ScheduledAt,
    SessionStatus Status,
    decimal DistanceMetres,
    string Target,
    SessionResult? Result);
