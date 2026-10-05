using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Horse_BackEnd.Services;

public sealed class HorseService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, ClubCalendar calendar, IOptions<BusinessOptions> options)
{
    public async Task<HorseRegistration> Create(RegistrationDraftRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.HorseOwner);
        var registration = new HorseRegistration { OwnerId = u.Id };
        await Apply(registration, r);
        db.Registrations.Add(registration);
        await events.Audit(AuditAction.RegistrationDraftCreated, registration.Id);
        await db.SaveChangesAsync(); return registration;
    }
    public async Task<HorseRegistration> Edit(Guid id, RegistrationDraftRequest r)
    {
        var reg = await access.Registration(id); var u = await current.Get();
        if (u.Role == Role.ClubManager)
        {
            Ensure.That(reg.Status == RegistrationStatus.PendingReview, Messages.Get(MessageKey.RegistrationCannotBeEditedInThisState), 409, "invalid_state");
            Ensure.That((r.Sire is null || r.Sire == reg.Sire) && (r.Dam is null || r.Dam == reg.Dam)
                && (!r.DateOfBirth.HasValue || r.DateOfBirth == reg.DateOfBirth) && (!r.Gender.HasValue || r.Gender == reg.Gender)
                && (r.Breed is null || r.Breed == reg.Breed) && (!r.HeightCm.HasValue || r.HeightCm == reg.HeightCm)
                && (!r.WeightKg.HasValue || r.WeightKg == reg.WeightKg) && (!r.MeasurementDate.HasValue || r.MeasurementDate == reg.MeasurementDate)
                && (r.DeclaredHealth is null || r.DeclaredHealth == reg.DeclaredHealth) && (r.HealthNotes is null || r.HealthNotes == reg.HealthNotes)
                && (!r.PreferredHeadTrainerId.HasValue || r.PreferredHeadTrainerId == reg.PreferredHeadTrainerId)
                && (!r.PreferredGroomId.HasValue || r.PreferredGroomId == reg.PreferredGroomId)
                && (!r.PreferredVeterinarianId.HasValue || r.PreferredVeterinarianId == reg.PreferredVeterinarianId),
                Messages.Get(MessageKey.ManagerMayOnlyEditAdministrativeRegistrationFields), 403, "forbidden");
            var before = new { reg.Name, reg.RegistrationNumber, reg.BoardingStart, reg.BoardingEnd };
            var administrative = Draft(reg) with { Name = r.Name ?? reg.Name, RegistrationNumber = r.RegistrationNumber ?? reg.RegistrationNumber,
                BoardingStart = r.BoardingStart ?? reg.BoardingStart, BoardingEnd = r.BoardingEnd ?? reg.BoardingEnd };
            await Apply(reg, administrative);
            await ValidateReady(reg);
            await events.Audit(AuditAction.RegistrationEdited, reg.Id, JsonSerializer.Serialize(new
            { Before = before, After = new { reg.Name, reg.RegistrationNumber, reg.BoardingStart, reg.BoardingEnd } }));
        }
        else
        {
            Ensure.Role(u, Role.HorseOwner);
            Ensure.That(reg.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.RegistrationCannotBeEditedInThisState), 409, "invalid_state");
            await Apply(reg, r); await events.Audit(AuditAction.RegistrationEdited, reg.Id);
        }
        await db.SaveChangesAsync(); return reg;
    }
    public async Task Submit(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HorseOwner);
        var r = await access.Registration(id);
        Ensure.That(r.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.OnlyDraftsOrRevisionsMayBeSubmitted), 409, "invalid_state");
        await ValidateReady(r);
        r.Status = RegistrationStatus.PendingReview; r.ReviewReason = null;
        await events.Managers(NotificationType.RegistrationReview, MessageKey.AHorseRegistrationRequiresReview, id);
        await events.Audit(AuditAction.RegistrationSubmitted, id);
        await db.SaveChangesAsync();
    }
    public async Task<RegistrationReviewResponse> Review(Guid id, ReviewRequest request)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager);
        var r = await access.Registration(id);
        Ensure.That(r.Status == RegistrationStatus.PendingReview, Messages.Get(MessageKey.RegistrationIsNotPendingReview), 409, "invalid_state");
        r.ReviewedBy = u.Id;
        if (!request.Approve)
        {
            Ensure.That(!string.IsNullOrWhiteSpace(request.Reason), Messages.Get(MessageKey.ARevisionReasonIsRequired));
            r.Status = RegistrationStatus.RevisionRequired; r.ReviewReason = request.Reason;
            events.Notify(r.OwnerId, NotificationType.RegistrationRevision, MessageKey.YourHorseRegistrationRequiresRevision, id);
            await events.Audit(AuditAction.RegistrationRevisionRequested, id);
            await db.SaveChangesAsync(); return new RegistrationReviewResponse(r,(Guid?)null );
        }
        await ValidateReady(r);
        r.Status = RegistrationStatus.Approved; r.ReviewReason = null;
        var horse = new Horse { RegistrationId = id, OwnerId = r.OwnerId, Name = r.Name!, Sire = r.Sire!, Dam = r.Dam!, DateOfBirth = r.DateOfBirth!.Value,
            Gender = r.Gender!.Value, Breed = r.Breed!, RegistrationNumber = r.RegistrationNumber, BoardingStart = r.BoardingStart!.Value, BoardingEnd = r.BoardingEnd };
        // Owner health is a declaration, not medical clearance; official status starts Monitoring.
        db.Horses.Add(horse);
        db.Measurements.Add(new Measurement { HorseId = horse.Id, Date = r.MeasurementDate!.Value, HeightCm = r.HeightCm!.Value, WeightKg = r.WeightKg!.Value });
        events.Notify(r.OwnerId, NotificationType.RegistrationApproved, MessageKey.YourHorseRegistrationWasApproved, horse.Id);
        await events.Audit(AuditAction.RegistrationApproved, id);
        await db.SaveChangesAsync(); return new RegistrationReviewResponse(r,(Guid?)horse.Id );
    }
    public async Task<StaffAssignment> Assign(Guid horseId, AssignmentRequest r)
    {
        var u = await current.Get(); var horse = await access.Horse(horseId);
        Ensure.That(r.StartDate != default && r.StartDate <= calendar.Today(clock), Messages.Get(MessageKey.AssignmentsStartTodayOrEarlierFutureSchedulingIsNot));
        if (r.Role == Role.Trainer)
        {
            Ensure.Role(u, Role.HeadTrainer);
            Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == u.Id && x.Active && x.Role == Role.HeadTrainer), Messages.Get(MessageKey.OnlyTheAssignedHeadTrainerCanAssignATrainer), 403, "forbidden");
        }
        else { Ensure.Role(u, Role.ClubManager); Ensure.That(r.Role is Role.HeadTrainer or Role.Groom or Role.Veterinarian, Messages.Get(MessageKey.UnsupportedAdministrativeAssignmentRole)); }
        await access.Staff(r.StaffId, r.Role);
        foreach (var old in await db.Assignments.Where(x => x.HorseId == horseId && x.Role == r.Role && x.Active).ToListAsync())
        {
            Ensure.That(r.StartDate >= old.StartDate, Messages.Get(MessageKey.ReplacementAssignmentCannotPrecedeTheCurrentAssignment));
            old.Active = false; old.EndDate = r.StartDate;
        }
        var a = new StaffAssignment { HorseId = horseId, StaffId = r.StaffId, Role = r.Role, StartDate = r.StartDate, Notes = r.Notes };
        db.Assignments.Add(a);
        events.Notify(r.StaffId, NotificationType.HorseAssignment, MessageKey.YouHaveBeenAssignedToAHorse, horseId);
        events.Notify(horse.OwnerId, NotificationType.HorseAssignment, MessageKey.OfficialHorseStaffAssignmentChanged, horseId);
        await events.Audit(AuditAction.HorseStaffAssigned, horseId, r.Role.ToString());
        await db.SaveChangesAsync(); return a;
    }
    private async Task Apply(HorseRegistration entity, RegistrationDraftRequest r)
    {
        Ensure.Validate(r);
        var today = calendar.Today(clock);
        Ensure.That(!r.DateOfBirth.HasValue || (r.DateOfBirth.Value != default && r.DateOfBirth <= today), Messages.Get(MessageKey.DateOfBirthMustBeValidAndNotIn));
        Ensure.That(!r.MeasurementDate.HasValue || (r.MeasurementDate.Value != default && r.MeasurementDate <= today && (!r.DateOfBirth.HasValue || r.MeasurementDate >= r.DateOfBirth)), Messages.Get(MessageKey.MeasurementDateIsInvalid));
        Ensure.That((!r.BoardingStart.HasValue || r.BoardingStart.Value != default) && (!r.BoardingEnd.HasValue || (r.BoardingEnd.Value != default && (!r.BoardingStart.HasValue || r.BoardingEnd >= r.BoardingStart))), Messages.Get(MessageKey.BoardingDatesAreInvalid));
        Ensure.That((!r.HeightCm.HasValue || r.HeightCm >= options.Value.MinHorseHeightCm && r.HeightCm <= options.Value.MaxHorseHeightCm)
            && (!r.WeightKg.HasValue || r.WeightKg >= options.Value.MinHorseWeightKg && r.WeightKg <= options.Value.MaxHorseWeightKg), Messages.Get(MessageKey.HorseMeasurementsOutsideConfiguredLimits));
        if (r.PreferredHeadTrainerId is Guid head) await access.Staff(head, Role.HeadTrainer);
        if (r.PreferredGroomId is Guid groom) await access.Staff(groom, Role.Groom);
        if (r.PreferredVeterinarianId is Guid vet) await access.Staff(vet, Role.Veterinarian);
        entity.Name = r.Name?.Trim(); entity.Sire = r.Sire?.Trim(); entity.Dam = r.Dam?.Trim(); entity.DateOfBirth = r.DateOfBirth;
        entity.Gender = r.Gender; entity.Breed = r.Breed; entity.RegistrationNumber = r.RegistrationNumber;
        entity.HeightCm = r.HeightCm; entity.WeightKg = r.WeightKg; entity.MeasurementDate = r.MeasurementDate;
        entity.DeclaredHealth = r.DeclaredHealth; entity.HealthNotes = r.HealthNotes;
        entity.BoardingStart = r.BoardingStart; entity.BoardingEnd = r.BoardingEnd;
        entity.PreferredHeadTrainerId = r.PreferredHeadTrainerId; entity.PreferredGroomId = r.PreferredGroomId; entity.PreferredVeterinarianId = r.PreferredVeterinarianId;
    }

    private async Task ValidateReady(HorseRegistration r)
    {
        var missing = new List<string>();
        foreach (var field in new[] { (nameof(r.Name), r.Name), (nameof(r.Sire), r.Sire), (nameof(r.Dam), r.Dam),
            (nameof(r.Breed), r.Breed), (nameof(r.DeclaredHealth), r.DeclaredHealth) })
            if (string.IsNullOrWhiteSpace(field.Item2)) missing.Add(field.Item1);
        foreach (var field in new (string Name, object? Value)[] { (nameof(r.DateOfBirth), r.DateOfBirth), (nameof(r.Gender), r.Gender),
            (nameof(r.HeightCm), r.HeightCm), (nameof(r.WeightKg), r.WeightKg), (nameof(r.MeasurementDate), r.MeasurementDate), (nameof(r.BoardingStart), r.BoardingStart) })
            if (field.Value is null) missing.Add(field.Name);
        Ensure.That(missing.Count == 0, Messages.Get(MessageKey.RegistrationMissingFields, string.Join(", ", missing)));
        await Apply(r, Draft(r));
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == r.Id && x.Type == AttachmentType.HorsePhoto), Messages.Get(MessageKey.AddAHorsePhotoBeforeSubmission));
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == r.Id && x.Type == AttachmentType.Certificate), Messages.Get(MessageKey.AddACertificateBeforeSubmission));
    }

    private static RegistrationDraftRequest Draft(HorseRegistration r) => new(r.Name, r.Sire, r.Dam, r.DateOfBirth, r.Gender,
        r.Breed, r.RegistrationNumber, r.HeightCm, r.WeightKg, r.MeasurementDate, r.DeclaredHealth, r.HealthNotes,
        r.BoardingStart, r.BoardingEnd, r.PreferredHeadTrainerId, r.PreferredGroomId, r.PreferredVeterinarianId);
}
