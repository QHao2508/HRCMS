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
    private static void Check(bool condition, string code) => Ensure.That(condition, Messages.Text(code switch
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
        Ensure.That(request.Specialties is not null && request.WorkingHours is not null && request.TimeOff is not null, Messages.Text(MessageKey.InvalidRequestBody));
        var specialties = request.Specialties!; var hours = request.WorkingHours!; var timeOff = request.TimeOff!;
        Ensure.That(specialties.Count <= Enum.GetValues<TrainingType>().Length && (staff.Role == Role.Trainer || specialties.Count == 0)
            && specialties.All(x => x is not null && Enum.IsDefined(x.TrainingType) && x.Level is >= 1 and <= 3)
            && specialties.Select(x => x.TrainingType).Distinct().Count() == specialties.Count
            && hours.Count <= 21 && hours.All(x => x is not null && Enum.IsDefined(x.DayOfWeek) && x.StartMinute >= 0 && x.EndMinute <= 1440 && x.EndMinute > x.StartMinute)
            && timeOff.Count <= 100 && timeOff.All(x => x is not null && x.Start < x.End), Messages.Text(MessageKey.InvalidRequestBody));
        var proposedExpertise = specialties.Select(x => new TrainerExpertise { StaffId = staffId, TrainingType = x.TrainingType, Level = x.Level }).ToList();
        foreach (var template in await training.TemplatesForTrainerAsync(staffId))
            Check(TemplateCurriculumPolicy.Matches(template, proposedExpertise), "trainer_specialty_mismatch");
        foreach (var day in hours.GroupBy(x => x.DayOfWeek))
        {
            var ordered = day.OrderBy(x => x.StartMinute).ToList();
            Ensure.That(!ordered.Zip(ordered.Skip(1)).Any(x => x.First.EndMinute > x.Second.StartMinute), Messages.Text(MessageKey.InvalidRequestBody));
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

    public async Task<TrainingAvailabilityResponse> Availability(Guid horseId, DateOnly date, int durationMinutes, TrainingType trainingType,
        Intensity intensity, decimal distanceMetres, Guid? riderId, Guid? exceptSessionId)
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        var horse = await access.Horse(horseId);
        Ensure.That(date != default && date < DateOnly.MaxValue && durationMinutes is >= 1 and <= 480 && Enum.IsDefined(trainingType) && Enum.IsDefined(intensity)
            && distanceMetres is > 0 and <= 100000, Messages.Text(MessageKey.InvalidRequestBody));
        if (exceptSessionId.HasValue)
        {
            var session = Ensure.Found(await training.FindSessionAsync(exceptSessionId.Value));
            Ensure.That(session.HorseId == horseId && session.Status is SessionStatus.Planned or SessionStatus.Assigned, Messages.Text(MessageKey.InvalidRequestBody));
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

    public async Task ValidateSession(Guid horseId, Guid? riderId, TrainingType trainingType, DateTimeOffset start, DateTimeOffset? end, Guid exceptSessionId, Guid? templateId = null, bool startingNow = false)
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
        await ValidateHours(trainer, start, end, startingNow);
        if (riderId.HasValue) await ValidateHours(await access.Staff(riderId.Value, Role.WorkRider), start, end, startingNow);
    }

    private async Task ValidateHours(User staff, DateTimeOffset start, DateTimeOffset? end, bool startingNow = false)
    {
        var hours = await repository.HoursAsync(staff.Id);
        if (hours.Count > 0)
        {
            if (startingNow)
            {
                // Execution may finish after the shift, but must start before its exclusive end.
                var day = calendar.DateAt(start);
                Check(hours.Any(x => x.DayOfWeek == day.DayOfWeek && calendar.At(day, x.StartMinute) <= start
                    && start < calendar.At(day, x.EndMinute)), "outside_working_hours");
            }
            else
            {
                Check(end.HasValue, "duration_required");
                Check(Covers(hours.Select(x => new WorkingHoursInput(x.DayOfWeek, x.StartMinute, x.EndMinute)), start, end!.Value), "outside_working_hours");
            }
        }
        Check((await repository.TimeOffAsync(staff.Id, start, end ?? start.AddTicks(1))).Count == 0, "staff_time_off");
    }

    // ===== Chọn Trainer cho ngựa: một bộ quy tắc cho cả danh sách ứng viên lẫn thao tác phân công =====

    /// <summary>Dữ liệu của ngựa: Trainer hiện tại, chuyên môn yêu cầu, buổi tập chưa xong và giáo án của chúng.</summary>
    private sealed record HorseTrainingContext(Guid HorseId, Guid? CurrentTrainerId, TrainingType? RequiredType,
        List<PendingTrainingSession> Sessions, Dictionary<Guid, TrainingTemplate> Templates, Dictionary<Guid, HashSet<Guid>> TemplateTrainers);

    /// <summary>Dữ liệu của các ứng viên, nạp theo lô một lần thay vì mỗi người, mỗi buổi tập một truy vấn.</summary>
    private sealed record CandidateSchedules(ILookup<Guid, TrainerExpertise> Expertise, ILookup<Guid, StaffWorkingHours> Hours,
        ILookup<Guid, StaffTimeOff> TimeOff, ILookup<Guid, ScheduledResource> OtherHorseSessions);

    private async Task<HorseTrainingContext> LoadHorseContext(Guid horseId, TrainingType? requiredType)
    {
        var sessions = await repository.PendingHorseSessionsAsync(horseId);
        var templates = new Dictionary<Guid, TrainingTemplate>(); var templateTrainers = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var templateId in sessions.Select(s => s.TemplateId).Distinct())
        {
            var template = Ensure.Found(await training.FindTemplateAsync(templateId));
            // Giáo án cũ chưa khai báo chương trình thì không ràng buộc chuyên môn (giữ tương thích dữ liệu cũ).
            if (TemplateCurriculumPolicy.Read(template) is null) continue;
            templates[templateId] = template;
            templateTrainers[templateId] = (await training.TemplateAssignmentsAsync(templateId)).Select(a => a.TrainerId).ToHashSet();
        }
        return new(horseId, await repository.CurrentTrainerAsync(horseId), requiredType, sessions, templates, templateTrainers);
    }

    private async Task<CandidateSchedules> LoadSchedules(HorseTrainingContext horse, Guid[] trainerIds)
    {
        var expertise = (await repository.ExpertiseAsync(trainerIds)).ToLookup(x => x.StaffId);
        if (horse.Sessions.Count == 0) return new(expertise, Empty<StaffWorkingHours>(), Empty<StaffTimeOff>(), Empty<ScheduledResource>());
        // Một khoảng thời gian bao trùm mọi buổi tập chưa xong của ngựa.
        var from = horse.Sessions.Min(s => calendar.DayRange(s.Start).Start);
        var to = horse.Sessions.Max(s => s.End ?? calendar.DayRange(s.Start).End);
        return new(expertise,
            (await repository.HoursAsync(trainerIds)).ToLookup(x => x.StaffId),
            (await repository.TimeOffAsync(trainerIds, from, to.AddTicks(1))).ToLookup(x => x.StaffId),
            (await repository.OtherHorseSessionsAsync(trainerIds, horse.HorseId, from, to)).ToLookup(x => x.TrainerId!.Value));
    }

    private static ILookup<Guid, T> Empty<T>() => Array.Empty<(Guid, T)>().ToLookup(x => x.Item1, x => x.Item2);

    /// <summary>
    /// Các lý do Trainer chưa nhận được ngựa, theo thứ tự kiểm tra; danh sách rỗng nghĩa là phù hợp.
    /// 1. Có chuyên môn Head Trainer yêu cầu. 2. Được giao và đủ chuyên môn cho giáo án của các kế hoạch đang chạy.
    /// 3. Với từng buổi tập chưa xong: không đang do người khác thực hiện, đúng chuyên môn, trong ca làm, không nghỉ phép, không trùng lịch ngựa khác.
    /// </summary>
    private List<TrainerFitIssue> Evaluate(HorseTrainingContext horse, CandidateSchedules data, Guid trainerId)
    {
        var issues = new List<TrainerFitIssue>();
        void Add(TrainerFitIssue issue) { if (!issues.Any(x => x.Text.Key == issue.Key && x.Text.Args.SequenceEqual(issue.Text.Args))) issues.Add(issue); }
        var skills = data.Expertise[trainerId].ToList();

        if (horse.RequiredType is { } required && !skills.Any(x => x.TrainingType == required))
            Add(new(MessageKey.TrainerSpecialtyMismatch, "trainer_specialty_mismatch"));

        foreach (var (templateId, template) in horse.Templates)
            if (!TemplateCurriculumPolicy.Matches(template, skills) || !horse.TemplateTrainers[templateId].Contains(trainerId))
                Add(new(MessageKey.TrainerNotQualifiedForTemplate, "template_trainer_mismatch", new object?[] { template.Name }));

        var hours = data.Hours[trainerId].Select(x => new WorkingHoursInput(x.DayOfWeek, x.StartMinute, x.EndMinute)).ToList();
        var timeOff = data.TimeOff[trainerId].ToList();
        var others = data.OtherHorseSessions[trainerId].ToList();
        foreach (var session in horse.Sessions)
        {
            if (session.InProgress && horse.CurrentTrainerId != trainerId)
                Add(new(MessageKey.TrainerSessionInProgress, "trainer_session_in_progress"));
            // Trainer chưa khai hồ sơ chuyên môn được giữ hành vi cũ: không chặn theo loại buổi tập.
            if (skills.Count > 0 && !skills.Any(x => x.TrainingType == session.TrainingType))
                Add(new(MessageKey.TrainerSpecialtyMismatch, "trainer_specialty_mismatch"));
            if (hours.Count > 0)
            {
                if (session.End is not { } end) Add(new(MessageKey.SchedulingDurationRequired, "duration_required"));
                else if (!Covers(hours, session.Start, end)) Add(new(MessageKey.StaffOutsideWorkingHours, "outside_working_hours"));
            }
            var until = session.End ?? session.Start.AddTicks(1);
            if (timeOff.Any(x => x.Start < until && x.End > session.Start))
                Add(new(MessageKey.StaffTimeOffConflict, "staff_time_off"));
            if (others.Any(x => Overlaps(x, session)))
                Add(new(MessageKey.TrainerScheduleOverlap, "trainer_schedule_overlap"));
        }
        return issues;
    }

    /// <summary>Buổi tập ở ngựa khác có chiếm cùng thời gian với buổi tập của ngựa này không (buổi chưa có giờ kết thúc tính theo giờ bắt đầu).</summary>
    private bool Overlaps(ScheduledResource other, PendingTrainingSession session)
    {
        if (other.InProgress) return true;
        // Chỉ xét buổi nằm trong ngày của buổi tập (đến giờ kết thúc nếu có), giống truy vấn lịch theo ngày.
        var day = calendar.DayRange(session.Start); var rangeEnd = session.End ?? day.End;
        if (!(other.Start < rangeEnd && (other.End is null ? other.Start >= day.Start : other.End > day.Start))) return false;
        return other.End is null
            ? session.End.HasValue ? other.Start >= session.Start && other.Start < session.End : other.Start == session.Start
            : session.End.HasValue ? other.Start < session.End && other.End > session.Start : other.Start <= session.Start && other.End > session.Start;
    }

    /// <summary>Danh sách Trainer cho ngựa: mỗi người kèm lý do chưa phù hợp, người phù hợp xếp trước.</summary>
    public async Task<List<TrainerCandidateResponse>> Candidates(Guid horseId, TrainingType trainingType)
    {
        var user = await current.Get(); Ensure.Role(user, Role.HeadTrainer, Role.ClubManager);
        await access.Horse(horseId);
        Check(Enum.IsDefined(trainingType), "invalid_training_type");
        if (user.Role == Role.HeadTrainer)
            Ensure.That(await repository.IsHeadTrainerAsync(horseId, user.Id), Messages.Text(MessageKey.PermissionDenied), 403, "forbidden");
        var trainers = await repository.TrainersAsync();
        var horse = await LoadHorseContext(horseId, trainingType);
        var data = await LoadSchedules(horse, trainers.Select(x => x.User.Id).ToArray());
        return trainers.Select(x =>
        {
            var skills = data.Expertise[x.User.Id].Select(e => new ExpertiseInput(e.TrainingType, e.Level)).ToList();
            var issues = Evaluate(horse, data, x.User.Id);
            return new TrainerCandidateResponse(x.User.Id, Name(x.User), skills, x.AssignedHorses, issues.Count == 0,
                skills.FirstOrDefault(e => e.TrainingType == trainingType)?.Level ?? 0,
                issues.Select(i => new CandidateIssue(i.Key.ToString(), i.Text.Args)).ToList());
        }).OrderByDescending(x => x.Matches).ThenByDescending(x => x.MatchLevel).ThenBy(x => x.AssignedHorses).ThenBy(x => x.Name).ToList();
    }

    /// <summary>Chặn phân công Trainer bằng lý do đầu tiên của <see cref="Evaluate"/> (cùng quy tắc với danh sách ứng viên).</summary>
    public async Task ValidateAssignment(Guid horseId, Guid trainerId, TrainingType? requiredType)
    {
        if (requiredType.HasValue) Check(Enum.IsDefined(requiredType.Value), "invalid_training_type");
        var horse = await LoadHorseContext(horseId, requiredType);
        var issue = Evaluate(horse, await LoadSchedules(horse, [trainerId]), trainerId).FirstOrDefault();
        if (issue is not null) Ensure.That(false, issue.Text, 409, issue.Code);
    }
}
