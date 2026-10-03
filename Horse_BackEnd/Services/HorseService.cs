using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Services;

public sealed class HorseService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
{
    public async Task<HorseRegistration> Create(RegistrationRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.HorseOwner);
        var registration = new HorseRegistration { OwnerId = u.Id };
        await Apply(registration, r);
        db.Registrations.Add(registration);
        await events.Audit(AuditAction.RegistrationDraftCreated, registration.Id);
        await db.SaveChangesAsync(); return registration;
    }
    public async Task<HorseRegistration> Edit(Guid id, RegistrationRequest r)
    {
        var reg = await access.Registration(id); var u = await current.Get();
        Ensure.That(reg.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired || (u.Role == Role.ClubManager && reg.Status == RegistrationStatus.PendingReview), "Registration cannot be edited in this state.", 409, "invalid_state");
        await Apply(reg, r); await events.Audit(AuditAction.RegistrationEdited, reg.Id);
        await db.SaveChangesAsync(); return reg;
    }
    public async Task Submit(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HorseOwner);
        var r = await access.Registration(id);
        Ensure.That(r.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, "Only drafts or revisions may be submitted.", 409, "invalid_state");
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == id && x.Type == AttachmentType.HorsePhoto), "Add a horse photo before submission.");
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == id && x.Type == AttachmentType.Certificate), "Add a certificate before submission.");
        r.Status = RegistrationStatus.PendingReview; r.ReviewReason = null;
        await events.Managers(NotificationType.RegistrationReview, "A horse registration requires review.", id);
        await events.Audit(AuditAction.RegistrationSubmitted, id);
        await db.SaveChangesAsync();
    }
    public async Task<object> Review(Guid id, ReviewRequest request)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager);
        var r = await access.Registration(id);
        Ensure.That(r.Status == RegistrationStatus.PendingReview, "Registration is not pending review.", 409, "invalid_state");
        r.ReviewedBy = u.Id;
        if (!request.Approve)
        {
            Ensure.That(!string.IsNullOrWhiteSpace(request.Reason), "A revision reason is required.");
            r.Status = RegistrationStatus.RevisionRequired; r.ReviewReason = request.Reason;
            events.Notify(r.OwnerId, NotificationType.RegistrationRevision, "Your horse registration requires revision.", id);
            await events.Audit(AuditAction.RegistrationRevisionRequested, id);
            await db.SaveChangesAsync(); return new { registration = r, horseId = (Guid?)null };
        }
        r.Status = RegistrationStatus.Approved; r.ReviewReason = null;
        var horse = new Horse { RegistrationId = id, OwnerId = r.OwnerId, Name = r.Name, Sire = r.Sire, Dam = r.Dam, DateOfBirth = r.DateOfBirth,
            Gender = r.Gender, Breed = r.Breed, RegistrationNumber = r.RegistrationNumber, BoardingStart = r.BoardingStart, BoardingEnd = r.BoardingEnd };
        // Owner health is a declaration, not medical clearance; official status starts Monitoring.
        db.Horses.Add(horse);
        db.Measurements.Add(new Measurement { HorseId = horse.Id, Date = r.MeasurementDate, HeightCm = r.HeightCm, WeightKg = r.WeightKg });
        events.Notify(r.OwnerId, NotificationType.RegistrationApproved, "Your horse registration was approved.", horse.Id);
        await events.Audit(AuditAction.RegistrationApproved, id);
        await db.SaveChangesAsync(); return new { registration = r, horseId = (Guid?)horse.Id };
    }
    public async Task<StaffAssignment> Assign(Guid horseId, AssignmentRequest r)
    {
        var u = await current.Get(); var horse = await access.Horse(horseId);
        Ensure.That(r.StartDate != default && r.StartDate <= calendar.Today(clock), "Assignments start today or earlier; future scheduling is not supported.");
        if (r.Role == Role.Trainer)
        {
            Ensure.Role(u, Role.HeadTrainer);
            Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == u.Id && x.Active && x.Role == Role.HeadTrainer), "Only the assigned Head Trainer can assign a Trainer.", 403, "forbidden");
        }
        else { Ensure.Role(u, Role.ClubManager); Ensure.That(r.Role is Role.HeadTrainer or Role.Groom or Role.Veterinarian, "Unsupported administrative assignment role."); }
        await access.Staff(r.StaffId, r.Role);
        foreach (var old in await db.Assignments.Where(x => x.HorseId == horseId && x.Role == r.Role && x.Active).ToListAsync())
        {
            Ensure.That(r.StartDate >= old.StartDate, "Replacement assignment cannot precede the current assignment.");
            old.Active = false; old.EndDate = r.StartDate;
        }
        var a = new StaffAssignment { HorseId = horseId, StaffId = r.StaffId, Role = r.Role, StartDate = r.StartDate, Notes = r.Notes };
        db.Assignments.Add(a);
        events.Notify(r.StaffId, NotificationType.HorseAssignment, "You have been assigned to a horse.", horseId);
        events.Notify(horse.OwnerId, NotificationType.HorseAssignment, "Official horse staff assignment changed.", horseId);
        await events.Audit(AuditAction.HorseStaffAssigned, horseId, r.Role.ToString());
        await db.SaveChangesAsync(); return a;
    }
    private async Task Apply(HorseRegistration entity, RegistrationRequest r)
    {
        var today = calendar.Today(clock);
        Ensure.That(r.DateOfBirth != default && r.DateOfBirth <= today, "Date of birth must be valid and not in the future.");
        Ensure.That(r.MeasurementDate >= r.DateOfBirth && r.MeasurementDate <= today, "Measurement date is invalid.");
        Ensure.That(r.BoardingStart != default && (!r.BoardingEnd.HasValue || r.BoardingEnd >= r.BoardingStart), "Boarding dates are invalid.");
        if (r.PreferredHeadTrainerId is Guid head) await access.Staff(head, Role.HeadTrainer);
        if (r.PreferredGroomId is Guid groom) await access.Staff(groom, Role.Groom);
        if (r.PreferredVeterinarianId is Guid vet) await access.Staff(vet, Role.Veterinarian);
        entity.Name = r.Name.Trim(); entity.Sire = r.Sire; entity.Dam = r.Dam; entity.DateOfBirth = r.DateOfBirth;
        entity.Gender = r.Gender; entity.Breed = r.Breed; entity.RegistrationNumber = r.RegistrationNumber;
        entity.HeightCm = r.HeightCm; entity.WeightKg = r.WeightKg; entity.MeasurementDate = r.MeasurementDate;
        entity.DeclaredHealth = r.DeclaredHealth; entity.HealthNotes = r.HealthNotes;
        entity.BoardingStart = r.BoardingStart; entity.BoardingEnd = r.BoardingEnd;
        entity.PreferredHeadTrainerId = r.PreferredHeadTrainerId; entity.PreferredGroomId = r.PreferredGroomId; entity.PreferredVeterinarianId = r.PreferredVeterinarianId;
    }
}
