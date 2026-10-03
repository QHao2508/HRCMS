using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Workflows;

public static class MedicalWorkflow
{
    public static async Task<object> GetSummary(Guid horseId, ClubAccess access, ClubDbContext db, TimeProvider clock)
    {
            var horse = await access.Horse(horseId); var now = clock.GetUtcNow();
            return new { horse.HealthStatus, restrictions = await db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now))
                .Select(x => new { x.Id, x.MedicalRecordId, x.TrainingLock, x.BlockAllTraining, x.MaxIntensity, x.MaxDistanceMetres, x.NoSprint, x.ValidFrom, x.ValidUntil, x.Reason }).ToListAsync() };
        }

    public static async Task<object> GetRecords(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.MedicalDetails(horseId); return await pager.Page(db.MedicalRecords.Where(x => x.HorseId == horseId).OrderByDescending(x => x.ExaminationAt), page, pageSize); }

    public static async Task<object> PostRecords(Guid horseId, MedicalRequest r, ClubAccess access, ClubDbContext db, CurrentUser current, ClubEvents events, TimeProvider clock)
    {
            await access.Vet(horseId); ValidateExamination(r, clock);
            var record = Record(horseId, (await current.Get()).Id, r);
            db.MedicalRecords.Add(record); (await access.Horse(horseId)).HealthStatus = r.HealthStatus;
            await events.Audit(AuditAction.MedicalExaminationCreated, record.Id);
            await events.HorseStaff(horseId, NotificationType.MedicalHealthChanged, MessageKey.HorseHealthStatusChangedReviewCurrentTrainingLimits, Role.Trainer, Role.HeadTrainer);
            await db.SaveChangesAsync(); return Results.Created($"/api/horses/{horseId}/medical/records", record);
        }

    public static async Task<object> GetInjuries(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.MedicalDetails(horseId); return await pager.Page(db.Injuries.Where(x => x.HorseId == horseId).OrderByDescending(x => x.InjuryDate), page, pageSize); }

    public static async Task<object> PutRecordsById(Guid horseId, Guid id, MedicalRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock)
    {
            await access.Vet(horseId); ValidateExamination(r, clock);
            var previous = Ensure.Found(await db.MedicalRecords.SingleOrDefaultAsync(x => x.Id == id && x.HorseId == horseId));
            Ensure.That(!await db.MedicalRecords.AnyAsync(x => x.SupersedesRecordId == id), Messages.Get(MessageKey.ACorrectionAlreadyExistsEditTheLatestRevision), 409, "invalid_state");
            var correction = Record(horseId, (await current.Get()).Id, r); correction.SupersedesRecordId = previous.Id;
            db.MedicalRecords.Add(correction);
            if (!await db.MedicalRecords.AnyAsync(x => x.HorseId == horseId && x.ExaminationAt > previous.ExaminationAt))
                (await access.Horse(horseId)).HealthStatus = correction.HealthStatus;
            await events.Audit(AuditAction.MedicalRecordCorrected, correction.Id); await db.SaveChangesAsync(); return correction;
        }

    public static async Task<object> PostInjuries(Guid horseId, InjuryRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
    {
            await access.Vet(horseId); await MedicalRecordFor(db, horseId, r.MedicalRecordId);
            Ensure.That(r.InjuryDate != default && r.InjuryDate <= calendar.Today(clock) && r.ReviewDate >= r.InjuryDate, Messages.Get(MessageKey.InvalidInjuryReviewDates));
            var injury = new Injury { HorseId = horseId, MedicalRecordId = r.MedicalRecordId, InjuryDate = r.InjuryDate, Type = r.Type, BodyLocation = r.BodyLocation, Severity = r.Severity, Cause = r.Cause, ReviewDate = r.ReviewDate };
            db.Injuries.Add(injury); (await access.Horse(horseId)).HealthStatus = HealthStatus.Injured;
            await events.Audit(AuditAction.MedicalInjuryCreated, injury.Id); await db.SaveChangesAsync(); return injury;
        }

    public static async Task<object> GetRestrictions(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.Horse(horseId); return await pager.Page(db.Restrictions.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt), page, pageSize); }

    public static async Task<object> PostRestrictions(Guid horseId, RestrictionRequest r, ClubAccess access, ClubDbContext db, ClubEvents events)
    {
            await access.Vet(horseId); await MedicalRecordFor(db, horseId, r.MedicalRecordId);
            Ensure.That(r.ValidFrom != default && (!r.ValidUntil.HasValue || r.ValidUntil >= r.ValidFrom), Messages.Get(MessageKey.InvalidRestrictionDates));
            Ensure.That(r.TrainingLock || r.BlockAllTraining || r.MaxIntensity.HasValue || r.MaxDistanceMetres.HasValue || r.NoSprint, Messages.Get(MessageKey.SetAtLeastOneRestriction));
            var restriction = new MedicalRestriction { HorseId = horseId, MedicalRecordId = r.MedicalRecordId, TrainingLock = r.TrainingLock, BlockAllTraining = r.BlockAllTraining,
                MaxIntensity = r.MaxIntensity, MaxDistanceMetres = r.MaxDistanceMetres, NoSprint = r.NoSprint, ValidFrom = r.ValidFrom.ToUniversalTime(), ValidUntil = r.ValidUntil?.ToUniversalTime(), Reason = r.Reason };
            db.Restrictions.Add(restriction);
            // The reason is an operational instruction visible to training roles; keep clinical diagnosis in MedicalRecord.
            await events.HorseStaff(horseId, NotificationType.MedicalRestrictionCreated, MessageKey.TrainingRestrictionsChangedReviewPlannedSessions, Role.Trainer, Role.HeadTrainer);
            var riders = await db.Sessions.Where(x => x.HorseId == horseId && x.Status == SessionStatus.InProgress && x.RiderId != null).Select(x => x.RiderId!.Value).Distinct().ToListAsync();
            foreach (var id in riders) events.Notify(id, NotificationType.MedicalRestrictionCreated, MessageKey.AMedicalRestrictionChangedDuringYourSessionStopIncompatible, horseId);
            await events.Audit(AuditAction.MedicalRestrictionCreated, restriction.Id); await db.SaveChangesAsync(); return restriction;
        }

    public static async Task<object> GetTreatments(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.MedicalDetails(horseId); return await pager.Page(db.Treatments.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt), page, pageSize); }

    public static async Task<object> PostTreatments(Guid horseId, TreatmentRequest r, ClubAccess access, ClubDbContext db, ClubEvents events)
    {
            await access.Vet(horseId); await MedicalRecordFor(db, horseId, r.MedicalRecordId);
            Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate && r.FollowUpDate >= r.StartDate, Messages.Get(MessageKey.InvalidTreatmentDates));
            if (r.InjuryId.HasValue) Ensure.That(await db.Injuries.AnyAsync(x => x.Id == r.InjuryId && x.HorseId == horseId && x.MedicalRecordId == r.MedicalRecordId), Messages.Get(MessageKey.InjuryDoesNotBelongToThisMedicalRecord));
            var treatment = new TreatmentPlan { HorseId = horseId, MedicalRecordId = r.MedicalRecordId, InjuryId = r.InjuryId, StartDate = r.StartDate, EndDate = r.EndDate, FollowUpDate = r.FollowUpDate,
                Objective = r.Objective, Instructions = r.Instructions, Medication = r.Medication, Frequency = r.Frequency };
            db.Treatments.Add(treatment); await events.Audit(AuditAction.MedicalTreatmentCreated, treatment.Id); await db.SaveChangesAsync(); return treatment;
        }

    public static async Task<object> GetFollowUps(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.MedicalDetails(horseId); return await pager.Page(db.FollowUps.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt), page, pageSize); }

    public static async Task<object> PostFollowUps(Guid horseId, FollowUpRequest r, ClubAccess access, ClubDbContext db, CurrentUser current, ClubEvents events, TimeProvider clock)
    {
            await access.Vet(horseId); await MedicalRecordFor(db, horseId, r.PreviousRecordId);
            Ensure.Validate(r.Examination); ValidateExamination(r.Examination, clock);
            Ensure.That(!r.Clearance || r.Examination.HealthStatus == HealthStatus.Fit, Messages.Get(MessageKey.ClearanceRequiresFitHealthStatus));
            var record = Record(horseId, (await current.Get()).Id, r.Examination); db.MedicalRecords.Add(record);
            (await access.Horse(horseId)).HealthStatus = r.Examination.HealthStatus;
            var follow = new MedicalFollowUp { HorseId = horseId, PreviousRecordId = r.PreviousRecordId, CurrentRecordId = record.Id, Clearance = r.Clearance, Outcome = r.Outcome };
            db.FollowUps.Add(follow);
            if (r.Clearance)
            {
                // Clearance is horse-wide: preserve all historical records while closing active restrictions/treatments.
                foreach (var restriction in await db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared).ToListAsync()) restriction.Cleared = true;
                foreach (var injury in await db.Injuries.Where(x => x.HorseId == horseId && x.Status == InjuryStatus.Active).ToListAsync()) injury.Status = InjuryStatus.Recovered;
                foreach (var treatment in await db.Treatments.Where(x => x.HorseId == horseId && !x.Completed).ToListAsync()) treatment.Completed = true;
            }
            await events.HorseStaff(horseId, NotificationType.MedicalFollowUp, r.Clearance ? MessageKey.MedicalClearanceIssuedReviewTheTrainingPlanBeforeResuming : MessageKey.MedicalFollowUpCompletedReviewCurrentRestrictions, Role.Trainer, Role.HeadTrainer);
            await events.Audit(r.Clearance ? AuditAction.MedicalClearanceIssued : AuditAction.MedicalFollowUp, follow.Id);
            await db.SaveChangesAsync(); return follow;
        }

    public static async Task<object> GetPreventiveCare(Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.Horse(horseId); return await pager.Page(db.PreventiveCare.Where(x => x.HorseId == horseId).OrderBy(x => x.DueDate).Select(x => new { x.Id, x.HorseId, x.Type, x.DueDate, x.CompletedDate }), page, pageSize); }

    public static async Task<object> PostPreventiveCare(Guid horseId, PreventiveRequest r, ClubAccess access, ClubDbContext db, ClubEvents events)
    {
            await access.Vet(horseId); Ensure.That(Enum.IsDefined(r.Type), Messages.Get(MessageKey.UnknownPreventiveCareType)); Ensure.That(r.DueDate != default, Messages.Get(MessageKey.DueDateIsRequired));
            var p = new PreventiveCare { HorseId = horseId, Type = r.Type, DueDate = r.DueDate, Notes = r.Notes }; db.PreventiveCare.Add(p);
            await events.Audit(AuditAction.MedicalPreventiveScheduled, p.Id); await db.SaveChangesAsync(); return p;
        }

    public static async Task<object> PostPreventiveCareByIdComplete(Guid horseId, Guid id, PreventiveCompletionRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
    {
            await access.Vet(horseId); var p = Ensure.Found(await db.PreventiveCare.SingleOrDefaultAsync(x => x.Id == id && x.HorseId == horseId));
            Ensure.That(!p.CompletedDate.HasValue && r.CompletedDate != default && r.CompletedDate <= calendar.Today(clock), Messages.Get(MessageKey.InvalidCompletion));
            p.CompletedDate = r.CompletedDate; p.Notes = r.Notes; await events.Audit(AuditAction.MedicalPreventiveCompleted, id); await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task MedicalRecordFor(ClubDbContext db, Guid horseId, Guid id)
        => Ensure.That(await db.MedicalRecords.AnyAsync(x => x.Id == id && x.HorseId == horseId), Messages.Get(MessageKey.MedicalRecordDoesNotBelongToThisHorse));
    public static void ValidateExamination(MedicalRequest r, TimeProvider clock)
        => Ensure.That(r.ExaminationAt != default && r.ExaminationAt <= clock.GetUtcNow(), Messages.Get(MessageKey.ExaminationDateCannotBeInTheFuture));
    public static MedicalRecord Record(Guid horseId, Guid vetId, MedicalRequest r)
        => new() { HorseId = horseId, VeterinarianId = vetId, ExaminationAt = r.ExaminationAt.ToUniversalTime(), Reason = r.Reason, Symptoms = r.Symptoms, Findings = r.Findings, Diagnosis = r.Diagnosis, HealthStatus = r.HealthStatus, Notes = r.Notes };
}
