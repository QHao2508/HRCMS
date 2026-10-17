using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record ExpertiseInput(TrainingType TrainingType, int Level);
public sealed record WorkingHoursInput(DayOfWeek DayOfWeek, int StartMinute, int EndMinute);
public sealed record TimeOffInput(DateTimeOffset Start, DateTimeOffset End);
public sealed record StaffTrainingProfileRequest(List<ExpertiseInput> Specialties, List<WorkingHoursInput> WorkingHours, List<TimeOffInput> TimeOff);
public sealed record StaffTrainingProfileResponse(Guid StaffId, List<ExpertiseInput> Specialties, List<WorkingHoursInput> WorkingHours, List<TimeOffInput> TimeOff);
public sealed record TrainerCandidateResponse(Guid Id, string Name, List<ExpertiseInput> Specialties, int AssignedHorses, bool Matches, int MatchLevel);
public sealed record TimeRange(DateTimeOffset Start, DateTimeOffset End);
public sealed record BusyRange(DateTimeOffset Start, DateTimeOffset End, string Reason);
public sealed record ResourceAvailability(Guid? Id, string Name, string State, List<TimeRange> Free, List<BusyRange> Busy);
public sealed record TrainingAvailabilityResponse(DateOnly Date, string TimeZone, int DurationMinutes,
    ResourceAvailability Horse, ResourceAvailability Trainer, ResourceAvailability? Rider, List<TimeRange> CommonFree);
