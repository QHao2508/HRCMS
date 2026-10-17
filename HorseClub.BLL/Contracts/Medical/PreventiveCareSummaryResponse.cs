using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record PreventiveCareSummaryResponse(
    Guid Id,
    Guid HorseId,
    PreventiveCareType Type,
    DateOnly DueDate,
    DateOnly? CompletedDate);
