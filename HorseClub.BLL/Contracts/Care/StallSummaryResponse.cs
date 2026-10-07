using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record StallSummaryResponse(
    Guid Id,
    Guid StableId,
    string Name,
    CleaningStatus CleaningStatus);
