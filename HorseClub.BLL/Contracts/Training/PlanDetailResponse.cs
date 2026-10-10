using HorseClub.DAL.Entities;
using HorseClub.DAL.Abstractions;

namespace HorseClub.BLL.Contracts;

public sealed record PlanDetailResponse(
    TrainingPlan Plan,
    List<TrainingSession> Sessions,
    List<MedicalRestriction> Restrictions,
    int SessionPage,
    int SessionPageSize,
    int SessionTotal, string? HorseName = null, string? TrainerName = null, List<TrainingNames>? SessionNames = null, bool HorseArchived = false);
