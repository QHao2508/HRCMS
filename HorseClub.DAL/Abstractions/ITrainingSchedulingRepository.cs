namespace HorseClub.DAL.Abstractions;

public sealed record ScheduledResource(Guid SessionId, Guid HorseId, Guid? TrainerId, Guid? RiderId,
    DateTimeOffset Start, DateTimeOffset? End, bool InProgress, TrainingType TrainingType);
public sealed record TrainerCandidateData(User User, int AssignedHorses);
/// <summary>Buổi tập chưa kết thúc của một ngựa, kèm giáo án của kế hoạch chứa buổi đó.</summary>
public sealed record PendingTrainingSession(Guid SessionId, Guid TemplateId, DateTimeOffset Start, DateTimeOffset? End, bool InProgress, TrainingType TrainingType);

public interface ITrainingSchedulingRepository
{
    ValueTask<User?> FindStaffAsync(Guid id);
    Task<Guid?> CurrentTrainerAsync(Guid horseId);
    Task<bool> IsHeadTrainerAsync(Guid horseId, Guid staffId);
    Task<List<TrainerCandidateData>> TrainersAsync();
    Task<List<TrainerExpertise>> ExpertiseAsync(Guid[] staffIds);
    Task<List<TrainingType>> RequiredTypesAsync(Guid staffId);
    Task<List<StaffWorkingHours>> HoursAsync(Guid staffId);
    Task<List<StaffWorkingHours>> HoursAsync(Guid[] staffIds);
    Task<List<StaffTimeOff>> TimeOffAsync(Guid staffId, DateTimeOffset from, DateTimeOffset to);
    Task<List<StaffTimeOff>> TimeOffAsync(Guid[] staffIds, DateTimeOffset from, DateTimeOffset to);
    Task<List<ScheduledResource>> ReservationsAsync(Guid horseId, Guid? trainerId, Guid? riderId, DateTimeOffset from, DateTimeOffset to, Guid? exceptSessionId);
    /// <summary>Buổi tập mở của các Trainer ở ngựa khác (để phát hiện trùng lịch khi nhận thêm ngựa).</summary>
    Task<List<ScheduledResource>> OtherHorseSessionsAsync(Guid[] trainerIds, Guid exceptHorseId, DateTimeOffset from, DateTimeOffset to);
    Task<List<PendingTrainingSession>> PendingHorseSessionsAsync(Guid horseId);
    Task ReplaceProfileAsync(Guid staffId, List<TrainerExpertise> expertise, List<StaffWorkingHours> hours, List<StaffTimeOff> timeOff);
}
