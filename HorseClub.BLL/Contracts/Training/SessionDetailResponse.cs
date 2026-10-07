using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record SessionDetailResponse(
    TrainingSession Session,
    SessionResult? Result,
    TrainerEvaluation? Evaluation);
