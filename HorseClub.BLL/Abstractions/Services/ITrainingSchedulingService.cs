using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

public interface ITrainingSchedulingService
{
    Task<System.Text.Json.JsonElement> ReferenceData();
    Task<StaffTrainingProfileResponse> GetProfile(Guid staffId);
    Task<StaffTrainingProfileResponse> UpdateProfile(Guid staffId, StaffTrainingProfileRequest request);
    Task<List<TrainerCandidateResponse>> Candidates(Guid horseId, TrainingType trainingType);
    Task<TrainingAvailabilityResponse> Availability(Guid horseId, DateOnly date, int durationMinutes, TrainingType trainingType,
        Intensity intensity, decimal distanceMetres, Guid? riderId, Guid? exceptSessionId);
    Task ValidateSession(Guid horseId, Guid? riderId, TrainingType trainingType, DateTimeOffset start, DateTimeOffset? end, Guid exceptSessionId, Guid? templateId = null, bool startingNow = false);
    Task ValidateAssignment(Guid horseId, Guid trainerId, TrainingType? requiredType);
}
