using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record SessionDetailResponse(
    TrainingSession Session,
    SessionResult? Result,
    TrainerEvaluation? Evaluation, string? HorseName = null, string? TrainerName = null, string? RiderName = null, bool HorseArchived = false);
