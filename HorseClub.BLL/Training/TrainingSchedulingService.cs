using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Training;

public sealed class TrainingSchedulingService(ITrainingSchedulingRepository repository, ITrainingRepository training,
    IUnitOfWork unitOfWork, CurrentUser current, ClubAccess access, ClubEvents events, ClubCalendar calendar,
    TimeProvider clock, IOptions<BusinessOptions> options, ITrainingTemplateService templates) : ITrainingSchedulingService
{
    private static string Name(User user) => (user.FirstName + " " + user.LastName).Trim();
    public async Task<System.Text.Json.JsonElement> ReferenceData()
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        return PublicRacingReference.Read();
    }
    private static void Check(bool condition, string code) => Ensure.That(condition, Messages.Get(code switch
    {
        "trainer_schedule_overlap" => MessageKey.TrainerScheduleOverlap,
        "trainer_specialty_mismatch" => MessageKey.TrainerSpecialtyMismatch,
        "outside_working_hours" => MessageKey.StaffOutsideWorkingHours,
        "staff_time_off" => MessageKey.StaffTimeOffConflict,
        "staff_profile_bookings_conflict" => MessageKey.StaffProfileBookingsConflict,
        "trainer_session_in_progress" => MessageKey.TrainerSessionInProgress,
        "duration_required" => MessageKey.SchedulingDurationRequired,
        "trainer_unassigned" => MessageKey.TrainerUnassigned,
        _ => MessageKey.InvalidRequestBody
    }), 409, code);

    public async Task<StaffTrainingProfileResponse> GetProfile(Guid staffId)
    {
        Ensure.Role(await current.Get(), Role.ClubManager);
        var staff = Ensure.Found(await repository.FindStaffAsync(staffId));
        Check(staff.Role is Role.Trainer or Role.WorkRider, "training_staff_required");
        return await Profile(staffId);
    }

    private async Task<StaffTrainingProfileResponse> Profile(Guid staffId) => new(staffId,
        (await repository.ExpertiseAsync([staffId])).Select(x => new ExpertiseInput(x.TrainingType, x.Level)).ToList(),
        (await repository.HoursAsync(staffId)).Select(x => new WorkingHoursInput(x.DayOfWeek, x.StartMinute, x.EndMinute)).ToList(),
        (await repository.TimeOffAsync(staffId, DateTimeOffset.MinValue, DateTimeOffset.MaxValue)).Select(x => new TimeOffInput(x.Start, x.End)).ToList());

    public async Task<StaffTrainingProfileResponse> UpdateProfile(Guid staffId, StaffTrainingProfileRequest request)
    {
        Ensure.Role(await current.Get(), Role.ClubManager);
        var staff = Ensure.Found(await repository.FindStaffAsync(staffId));
        Check(staff.Role is Role.Trainer or Role.WorkRider, "training_staff_required");
        Ensure.That(request.Specialties is not null && request.WorkingHours is not null && request.TimeOff is not null, Messages.Get(MessageKey.InvalidRequestBody));
        var specialties = request.Specialties!; var hours = request.WorkingHours!; var timeOff = request.TimeOff!;
        Ensure.That(specialties.Count <= Enum.GetValues<TrainingType>().Length && (staff.Role == Role.Trainer || specialties.Count == 0)
            && specialties.All(x => x is not null && Enum.IsDefined(x.TrainingType) && x.Level is >= 1 and <= 3)
            && specialties.Select(x => x.TrainingType).Distinct().Count() == specialties.Count
            && hours.Count <= 21 && hours.All(x => x is not null && Enum.IsDefined(x.DayOfWeek) && x.StartMinute >= 0 && x.EndMinute <= 1440 && x.EndMinute > x.StartMinute)
            && timeOff.Count <= 100 && timeOff.All(x => x is not null && x.Start < x.End), Messages.Get(MessageKey.InvalidRequestBody));
        foreach (var day in hours.GroupBy(x => x.DayOfWeek))
        {
            var ordered = day.OrderBy(x => x.StartMinute).ToList();
            Ensure.That(!ordered.Zip(ordered.Skip(1)).Any(x => x.First.EndMinute > x.Second.StartMinute), Messages.Get(MessageKey.InvalidRequestBody));
        }
        // Refuse profile edits that would invalidate booked future sessions or active work.
        foreach (var requiredType in await repository.RequiredTypesAsync(staffId))
            Check(specialties.Any(x => x.TrainingType == requiredType), "trainer_specialty_mismatch");
        var pending = await repository.ReservationsAsync(Guid.Empty, staffId, staffId, clock.GetUtcNow(), DateTimeOffset.MaxValue, null);
        foreach (var reservation in pending)
        {
            if (staff.Role == Role.Trainer) Check(specialties.Any(x => x.TrainingType == reservation.TrainingType), "trainer_specialty_mismatch");
            if (reservation.End is { } end)
                Check(Covers(hours, reservation.Start, end) && !timeOff.Any(x => x.Start < end && x.End > reservation.Start), "staff_profile_bookings_conflict");
            else Check(!reservation.InProgress && !timeOff.Any(x => x.Start <= reservation.Start && x.End > reservation.Start), "staff_profile_bookings_conflict");
        }
        await repository.ReplaceProfileAsync(staffId,
            specialties.Select(x => new TrainerExpertise { StaffId = staffId, TrainingType = x.TrainingType, Level = x.Level }).ToList(),
            hours.Select(x => new StaffWorkingHours { StaffId = staffId, DayOfWeek = x.DayOfWeek, StartMinute = x.StartMinute, EndMinute = x.EndMinute }).ToList(),
            timeOff.Select(x => new StaffTimeOff { StaffId = staffId, Start = x.Start.ToUniversalTime(), End = x.End.ToUniversalTime() }).ToList());
        await events.Audit(AuditAction.StaffTrainingProfileUpdated, staffId);
        await unitOfWork.SaveChangesAsync();
        return await Profile(staffId);
    }

    public async Task<List<TrainerCandidateResponse>> Candidates(Guid horseId, TrainingType trainingType)
    {
        var user = await current.Get(); Ensure.Role(user, Role.HeadTrainer, Role.ClubManager);
        await access.Horse(horseId);
        Check(Enum.IsDefined(trainingType), "invalid_training_type");
        if (user.Role == Role.HeadTrainer)
            Ensure.That(await repository.IsHeadTrainerAsync(horseId, user.Id), Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
        var trainers = await repository.TrainersAsync();
        var expertise = await repository.ExpertiseAsync(trainers.Select(x => x.User.Id).ToArray());
        return trainers.Select(x =>
        {
            var skills = expertise.Where(e => e.StaffId == x.User.Id).Select(e => new ExpertiseInput(e.TrainingType, e.Level)).ToList();
            var level = skills.FirstOrDefault(e => e.TrainingType == trainingType)?.Level ?? 0;
            return new TrainerCandidateResponse(x.User.Id, Name(x.User), skills, x.AssignedHorses, level > 0, level);
        }).OrderByDescending(x => x.Matches).ThenByDescending(x => x.MatchLevel).ThenBy(x => x.AssignedHorses).ThenBy(x => x.Name).ToList();
    }

    public async Task<TrainingAvailabilityResponse> Availability(Guid horseId, DateOnly date, int durationMinutes, TrainingType trainingType,
        Intensity intensity, decimal distanceMetres, Guid? riderId, Guid? exceptSessionId)
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        var horse = await access.Horse(horseId);
        Ensure.That(date != default && date < DateOnly.MaxValue && durationMinutes is >= 1 and <= 480 && Enum.IsDefined(trainingType) && Enum.IsDefined(intensity)
            && distanceMetres is > 0 and <= 100000, Messages.Get(MessageKey.InvalidRequestBody));
        if (exceptSessionId.HasValue)
        {
            var session = Ensure.Found(await training.FindSessionAsync(exceptSessionId.Value));
            Ensure.That(session.HorseId == horseId && session.Status is SessionStatus.Planned or SessionStatus.Assigned, Messages.Get(MessageKey.InvalidRequestBody));
        }
        var start = calendar.At(date, 0); var end = calendar.At(date.AddDays(1), 0);
        var now = clock.GetUtcNow();
        var roundedNow = new DateTimeOffset((now.UtcTicks + TimeSpan.TicksPerMinute - 1) / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute, TimeSpan.Zero);
        var earliest = roundedNow > start ? roundedNow : start;
        var windows = earliest < end ? new List<TimeRange> { new(earliest, end) } : [];
        var trainerId = await repository.CurrentTrainerAsync(horseId);
        var rider = riderId.HasValue ? await access.Staff(riderId.Value, Role.WorkRider) : null;
        var reservations = await repository.ReservationsAsync(horseId, trainerId, riderId, start, end, exceptSessionId);
        var horseBusy = Busy(reservations.Where(x => x.HorseId == horseId), start, end);
        if (horse.HealthStatus == HealthStatus.Isolated || intensity == Intensity.Heavy && (horse.HealthStatus == HealthStatus.Injured || await training.HasUnrecoveredInjuryAsync(horseId, date)))
            horseBusy.Add(new(start, end, "medical_block"));
        foreach (var restriction in await training.GetRestrictionsAsync(horseId, start, end))
            if (restriction.BlockAllTraining || restriction.TrainingLock && intensity == Intensity.Heavy || restriction.MaxIntensity.HasValue && intensity > restriction.MaxIntensity
                || restriction.MaxDistanceMetres.HasValue && distanceMetres > restriction.MaxDistanceMetres || restriction.NoSprint && trainingType == TrainingType.Sprint)
                horseBusy.Add(new(restriction.ValidFrom > start ? restriction.ValidFrom : start,
                    restriction.ValidUntil is { } until && until < end ? until.AddTicks(1) : end, "medical_restriction"));
        var horseState = horseBusy.Any(b => b.Reason == "duration_unknown") ? "Unknown" : "Known";
        var horseAvailability = new ResourceAvailability(horse.Id, horse.Name, horseState, AvailabilityRanges.ForMinuteInput(AvailabilityRanges.Subtract(windows, horseBusy, durationMinutes), durationMinutes), horseBusy);
        var trainer = trainerId.HasValue ? Ensure.Found(await repository.FindStaffAsync(trainerId.Value)) : null;
        var trainerAvailability = trainer is null ? new ResourceAvailability(null, "", "Unassigned", [], [])
            : await StaffAvailability(trainer, date, windows, start, end, durationMinutes, reservations.Where(x => x.TrainerId == trainer.Id), trainingType);
        var riderAvailability = rider is null ? null : await StaffAvailability(rider, date, windows, start, end, durationMinutes, reservations.Where(x => x.RiderId == rider.Id), null);
        var common = AvailabilityRanges.Intersect(horseAvailability.Free, trainerAvailability.Free, durationMinutes);
        if (riderAvailability is not null) common = AvailabilityRanges.Intersect(common, riderAvailability.Free, durationMinutes);
        return new(date, options.Value.TimeZoneId, durationMinutes, horseAvailability, trainerAvailability, riderAvailability, common);
    }

    private static List<BusyRange> Busy(IEnumerable<ScheduledResource> reservations, DateTimeOffset start, DateTimeOffset end)
        => reservations.Select(x => x.InProgress ? new BusyRange(start, end, "in_progress")
            : x.End is { } finish ? new BusyRange(x.Start < start ? start : x.Start, finish > end ? end : finish, "session") : new BusyRange(start, end, "duration_unknown")).ToList();

    private async Task<ResourceAvailability> StaffAvailability(User staff, DateOnly date, List<TimeRange> windows, DateTimeOffset start, DateTimeOffset end,
        int durationMinutes, IEnumerable<ScheduledResource> reservations, TrainingType? requiredType)
    {
        var hours = await repository.HoursAsync(staff.Id);
        var busy = Busy(reservations, start, end);
        busy.AddRange((await repository.TimeOffAsync(staff.Id, start, end)).Select(x => new BusyRange(x.Start < start ? start : x.Start, x.End > end ? end : x.End, "time_off")));
        if (!staff.Active || !staff.EmailVerified) return new(staff.Id, Name(staff), "Inactive", [], busy);
        if (requiredType.HasValue && !(await repository.ExpertiseAsync([staff.Id])).Any(x => x.TrainingType == requiredType))
            return new(staff.Id, Name(staff), "SpecialtyMissing", [], busy);
        if (hours.Count == 0) return new(staff.Id, Name(staff), "HoursMissing", [], busy);
        var shifts = hours.Where(x => x.DayOfWeek == date.DayOfWeek).Select(x => new TimeRange(calendar.At(date, x.StartMinute), calendar.At(date, x.EndMinute))).ToList();
        var free = AvailabilityRanges.ForMinuteInput(AvailabilityRanges.Subtract(AvailabilityRanges.Intersect(shifts, windows, durationMinutes), busy, durationMinutes), durationMinutes);
        return new(staff.Id, Name(staff), busy.Any(x => x.Reason == "duration_unknown") ? "Unknown" : "Known", free, busy);
    }

    private bool Covers(IEnumerable<WorkingHoursInput> hours, DateTimeOffset start, DateTimeOffset end)
    {
        var day = calendar.DateAt(start);
        return hours.Any(x => x.DayOfWeek == day.DayOfWeek && calendar.At(day, x.StartMinute) <= start && calendar.At(day, x.EndMinute) >= end);
    }

    public async Task ValidateSession(Guid horseId, Guid? riderId, TrainingType trainingType, DateTimeOffset start, DateTimeOffset? end, Guid exceptSessionId, Guid? templateId = null)
    {
        var trainerId = await repository.CurrentTrainerAsync(horseId);
        Check(trainerId.HasValue, "trainer_unassigned");
        var trainer = await access.Staff(trainerId!.Value, Role.Trainer);
        Check(trainer.EmailVerified, "training_staff_required");
        if (templateId.HasValue) await templates.ValidateTrainer(templateId.Value, trainer.Id);
        var expertise = await repository.ExpertiseAsync([trainer.Id]);
        // Preserve legacy API scheduling while making configured profiles authoritative.
        if (expertise.Count > 0) Check(expertise.Any(x => x.TrainingType == trainingType), "trainer_specialty_mismatch");
        var range = calendar.DayRange(start);
        var reservations = await repository.ReservationsAsync(horseId, trainerId, riderId, range.Start, end ?? range.End, exceptSessionId);
        foreach (var r in reservations)
        {
            var conflict = r.InProgress || (r.End is null
                ? end.HasValue ? r.Start >= start && r.Start < end : r.Start == start
                : end.HasValue ? r.Start < end && r.End > start : r.Start <= start && r.End > start);
            if (r.TrainerId == trainerId) Check(!conflict, "trainer_schedule_overlap");
        }
        await ValidateHours(trainer, start, end);
        if (riderId.HasValue) await ValidateHours(await access.Staff(riderId.Value, Role.WorkRider), start, end);
    }

    private async Task ValidateHours(User staff, DateTimeOffset start, DateTimeOffset? end)
    {
        var hours = await repository.HoursAsync(staff.Id);
        if (hours.Count > 0)
        {
            Check(end.HasValue, "duration_required");
            Check(Covers(hours.Select(x => new WorkingHoursInput(x.DayOfWeek, x.StartMinute, x.EndMinute)), start, end!.Value), "outside_working_hours");
        }
        Check((await repository.TimeOffAsync(staff.Id, start, end ?? start.AddTicks(1))).Count == 0, "staff_time_off");
    }

    public async Task ValidateAssignment(Guid horseId, Guid trainerId, TrainingType? requiredType)
    {
        var expertise = await repository.ExpertiseAsync([trainerId]);
        if (requiredType.HasValue)
        {
            Check(Enum.IsDefined(requiredType.Value), "invalid_training_type");
            Check(expertise.Any(x => x.TrainingType == requiredType), "trainer_specialty_mismatch");
        }
        foreach (var session in await repository.PendingHorseSessionsAsync(horseId))
        {
            var pendingSession = Ensure.Found(await training.FindSessionAsync(session.SessionId));
            var pendingPlan = Ensure.Found(await training.FindPlanAsync(pendingSession.PlanId));
            await templates.ValidateTrainer(pendingPlan.TemplateId, trainerId);
            Check(!session.InProgress || await repository.CurrentTrainerAsync(horseId) == trainerId, "trainer_session_in_progress");
            if (expertise.Count > 0) Check(expertise.Any(x => x.TrainingType == session.TrainingType), "trainer_specialty_mismatch");
            await ValidateHours(Ensure.Found(await repository.FindStaffAsync(trainerId)), session.Start, session.End);
            var range = calendar.DayRange(session.Start);
            var other = await repository.ReservationsAsync(Guid.Empty, trainerId, null, range.Start, session.End ?? range.End, session.SessionId);
            Check(!other.Any(x => x.HorseId != horseId && (x.InProgress || (x.End is null
                ? session.End.HasValue ? x.Start >= session.Start && x.Start < session.End : x.Start == session.Start
                : session.End.HasValue ? x.Start < session.End && x.End > session.Start : x.Start <= session.Start && x.End > session.Start))), "trainer_schedule_overlap");
        }
    }
}
