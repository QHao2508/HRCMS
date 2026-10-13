namespace HorseClub.DAL.Abstractions;

public sealed record ScheduledResource(Guid SessionId, Guid HorseId, Guid? TrainerId, Guid? RiderId,
    DateTimeOffset Start, DateTimeOffset? End, bool InProgress, TrainingType TrainingType);
public sealed record TrainerCandidateData(User User, int AssignedHorses);

public interface ITrainingSchedulingRepository
{
    ValueTask<User?> FindStaffAsync(Guid id);
    Task<Guid?> CurrentTrainerAsync(Guid horseId);
    Task<bool> IsHeadTrainerAsync(Guid horseId, Guid staffId);
    Task<List<TrainerCandidateData>> TrainersAsync();
    Task<List<TrainerExpertise>> ExpertiseAsync(Guid[] staffIds);
    Task<List<TrainingType>> RequiredTypesAsync(Guid staffId);
    Task<List<StaffWorkingHours>> HoursAsync(Guid staffId);
    Task<List<StaffTimeOff>> TimeOffAsync(Guid staffId, DateTimeOffset from, DateTimeOffset to);
    Task<List<ScheduledResource>> ReservationsAsync(Guid horseId, Guid? trainerId, Guid? riderId, DateTimeOffset from, DateTimeOffset to, Guid? exceptSessionId);
    Task<List<ScheduledResource>> PendingHorseSessionsAsync(Guid horseId);
    Task ReplaceProfileAsync(Guid staffId, List<TrainerExpertise> expertise, List<StaffWorkingHours> hours, List<StaffTimeOff> timeOff);
}
