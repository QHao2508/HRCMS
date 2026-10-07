using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record RestrictionSummaryResponse(
    Guid Id,
    Guid MedicalRecordId,
    bool TrainingLock,
    bool BlockAllTraining,
    Intensity? MaxIntensity,
    decimal? MaxDistanceMetres,
    bool NoSprint,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string Reason);
