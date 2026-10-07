using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record MedicalSummaryResponse(
    HealthStatus HealthStatus,
    List<RestrictionSummaryResponse> Restrictions);
