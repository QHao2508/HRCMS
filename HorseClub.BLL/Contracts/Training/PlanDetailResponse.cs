using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record PlanDetailResponse(
    TrainingPlan Plan,
    List<TrainingSession> Sessions,
    List<MedicalRestriction> Restrictions,
    int SessionPage,
    int SessionPageSize,
    int SessionTotal);
