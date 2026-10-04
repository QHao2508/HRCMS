using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Horse_BackEnd.Infrastructure;

public sealed class ApiException(int status, string code, string message, Guid? referenceId = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public Guid? ReferenceId { get; } = referenceId;
}

public static class Ensure
{
    public static void That(bool condition, string message, int status = 400, string code = "validation_error")
    { if (!condition) throw new ApiException(status, code, message); }
    public static T Found<T>(T? value) where T : class => value ?? throw new ApiException(404, "not_found", Messages.Get(MessageKey.RecordNotFound));
    public static void Role(User user, params Role[] roles) => That(roles.Contains(user.Role), Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
    public static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ApiException(400, "validation_error", string.Join(" ", errors.Select(x => Messages.Get(MessageKey.Invalid, string.Join(", ", x.MemberNames)))));
        foreach (var property in value.GetType().GetProperties())
        {
            var p = property.GetValue(value);
            if (p is string s) That(s.Length <= 4000, Messages.Get(MessageKey.Exceeds4000Characters, property.Name));
            if (p is Enum e) That(Enum.IsDefined(e.GetType(), e), Messages.Get(MessageKey.Invalid, property.Name));
            if (p?.GetType().Namespace == "Horse_BackEnd.Contracts") Validate(p);
        }
    }
}

public sealed class CurrentUser(ClubDbContext db, IHttpContextAccessor accessor)
{
    private User? cached;
    public async Task<User> Get()
    {
        if (cached is not null) return cached;
        var principal = accessor.HttpContext?.User;
        Ensure.That(Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id), Messages.Get(MessageKey.AuthenticationRequired), 401, "unauthorized");
        var user = await db.Users.FindAsync(id);
        Ensure.That(user is { Active: true, EmailVerified: true } && principal?.FindFirstValue("stamp") == user.SecurityStamp,
            Messages.Get(MessageKey.AccountOrTokenIsNoLongerValid), 401, "unauthorized");
        return cached = user!;
    }
}

public sealed class ClubAccess(ClubDbContext db, CurrentUser current)
{
    public async Task<Horse> Horse(Guid id, bool allowArchived = false)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await db.Horses.FindAsync(id));
        if (horse.Archived && !allowArchived) throw new ApiException(409, "horse_archived", Messages.Get(MessageKey.HorseIsArchived));
        if (user.Role == Role.ClubManager) return horse;
        if (user.Role == Role.HorseOwner && horse.OwnerId == user.Id) return horse;
        if (await db.Assignments.AnyAsync(x => x.HorseId == id && x.StaffId == user.Id && x.Active)) return horse;
        // Riders only see horses with sessions specifically assigned to them.
        if (user.Role == Role.WorkRider && await db.Sessions.AnyAsync(x => x.HorseId == id && x.RiderId == user.Id)) return horse;
        throw new ApiException(403, "forbidden", Messages.Get(MessageKey.HorseIsOutsideYourAssignedScope));
    }
    public async Task<IQueryable<Horse>> Horses()
    {
        var u = await current.Get();
        var q = db.Horses.Where(x => !x.Archived);
        if (u.Role == Role.ClubManager) return q;
        if (u.Role == Role.HorseOwner) return q.Where(x => x.OwnerId == u.Id);
        if (u.Role == Role.WorkRider) return q.Where(x => db.Sessions.Any(s => s.HorseId == x.Id && s.RiderId == u.Id));
        return q.Where(x => db.Assignments.Any(a => a.HorseId == x.Id && a.StaffId == u.Id && a.Active));
    }
    public async Task Trainer(Guid horseId)
    {
        var u = await current.Get();
        Ensure.Role(u, Role.Trainer);
        await Horse(horseId);
        Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == u.Id && x.Role == Role.Trainer && x.Active),
            Messages.Get(MessageKey.OnlyTheCurrentAssignedTrainerMayChangeTraining), 403, "forbidden");
    }
    public async Task Vet(Guid horseId)
    {
        Ensure.Role(await current.Get(), Role.Veterinarian);
        await Horse(horseId);
    }
    public async Task<User> Staff(Guid id, Role role)
    {
        var user = Ensure.Found(await db.Users.FindAsync(id));
        Ensure.That(user.Active && user.Role == role, Messages.Get(MessageKey.StaffMustBeAnActive, role));
        return user;
    }
    public async Task<HorseRegistration> Registration(Guid id)
    {
        var u = await current.Get();
        var r = Ensure.Found(await db.Registrations.FindAsync(id));
        Ensure.That(u.Role == Role.ClubManager || (u.Role == Role.HorseOwner && r.OwnerId == u.Id), Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
        return r;
    }
    public async Task MedicalDetails(Guid horseId)
    {
        // Manager gets aggregate/operational summaries, not clinical notes.
        await Vet(horseId);
    }
}

public sealed class ClubEvents(ClubDbContext db, CurrentUser current)
{
    public async Task TrainingHistory(TrainingPlan plan, TrainingSession? session = null)
    {
        var snapshot = JsonSerializer.Serialize<object>(session is null ? plan : session,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } });
        db.TrainingRevisions.Add(new TrainingRevision { PlanId = plan.Id, SessionId = session?.Id, ActorId = (await current.Get()).Id, Snapshot = snapshot });
    }
    public async Task Audit(AuditAction action, Guid referenceId, string detail = "")
    {
        var u = await current.Get();
        db.Audit.Add(new AuditEvent { ActorId = u.Id, Action = action, ReferenceId = referenceId, Detail = detail });
    }
    public void Notify(Guid recipient, NotificationType type, MessageKey message, Guid? reference = null)
        => db.Notifications.Add(new Notification { RecipientId = recipient, Type = type, Message = Messages.Get(message), ReferenceId = reference });
    public async Task Managers(NotificationType type, MessageKey message, Guid reference)
    {
        foreach (var id in await db.Users.Where(x => x.Active && x.Role == Role.ClubManager).Select(x => x.Id).ToListAsync()) Notify(id, type, message, reference);
    }
    public async Task HorseStaff(Guid horseId, NotificationType type, MessageKey message, params Role[] roles)
    {
        foreach (var id in await db.Assignments.Where(x => x.HorseId == horseId && x.Active && roles.Contains(x.Role)).Select(x => x.StaffId).Distinct().ToListAsync())
            Notify(id, type, message, horseId);
    }
}

public sealed class WriteGate { public SemaphoreSlim Semaphore { get; } = new(1, 1); }

public sealed class PageReader(IOptions<BusinessOptions> options)
{
    public async Task<PageResponse<T>> Page<T>(IQueryable<T> q, int? page, int? pageSize)
    {
        var p = page ?? 1; var size = pageSize ?? options.Value.DefaultPageSize;
        Ensure.That(size >= 1 && size <= options.Value.MaxPageSize && p >= 1 && p <= int.MaxValue / size, Messages.Get(MessageKey.PageMustBePositiveAndPageSizeBetween1And, options.Value.MaxPageSize));
        return new PageResponse<T>(await q.Skip((p - 1) * size).Take(size).ToListAsync(), p, size, await q.CountAsync());
    }
}
