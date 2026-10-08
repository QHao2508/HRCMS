using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by TrainingSessionService.</summary>
public interface ITrainingSessionService
{
    Task Guard(Guid horseId, decimal distance, Intensity intensity, TrainingType type, DateTimeOffset at);
    Task<TrainingSession> CreateSession(Guid planId, SessionRequest r);
    Task<TrainingSession> EditSession(Guid id, SessionRequest r);
    Task Start(Guid id);
    Task<SessionResult> SubmitResult(Guid id, ResultRequest r);
    Task<OperationResult> Create(Guid id, SessionRequest r);
    Task<PageResponse<TrainingSession>> ListSessions(Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize);
    Task<SessionDetailResponse> GetSession(Guid id);
    Task<TrainingSession> Update(Guid id, SessionRequest r);
    Task<TrainingSession> AssignRider(Guid id, RiderRequest r);
    Task<OperationResult> StartSession(Guid id);
    Task<SessionResult> RecordResult(Guid id, ResultRequest r);
    Task<OperationResult> SkipSession(Guid id, ReasonRequest r);
    Task<TrainerEvaluation> EvaluateSession(Guid id, EvaluationRequest r);
}
