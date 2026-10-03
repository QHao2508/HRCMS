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
    public static T Found<T>(T? value) where T : class => value ?? throw new ApiException(404, "not_found", "Record not found.");
    public static void Role(User user, params Role[] roles) => That(roles.Contains(user.Role), "Permission denied.", 403, "forbidden");
    public static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ApiException(400, "validation_error", string.Join(" ", errors.Select(x => x.ErrorMessage)));
        foreach (var property in value.GetType().GetProperties())
        {
            var p = property.GetValue(value);
            if (p is string s) That(s.Length <= 4000, $"{property.Name} exceeds 4000 characters.");
            if (p is Enum e) That(Enum.IsDefined(e.GetType(), e), $"Invalid {property.Name}.");
        }
    }
}

public sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
            if (arg?.GetType().Namespace == "Horse_BackEnd.Contracts") Ensure.Validate(arg);
        return await next(context);
    }
}

public sealed class AllowedRolesFilter(params Role[] roles) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await context.HttpContext.RequestServices.GetRequiredService<CurrentUser>().Get();
        Ensure.Role(user, roles);
        return await next(context);
    }
}

public sealed class CurrentUser(ClubDbContext db, IHttpContextAccessor accessor)
{
    private User? cached;
    public async Task<User> Get()
    {
        if (cached is not null) return cached;
        var principal = accessor.HttpContext?.User;
        Ensure.That(Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id), "Authentication required.", 401, "unauthorized");
        var user = await db.Users.FindAsync(id);
        Ensure.That(user is { Active: true, EmailVerified: true } && principal?.FindFirstValue("stamp") == user.SecurityStamp,
            "Account or token is no longer valid.", 401, "unauthorized");
        return cached = user!;
    }
}

public sealed class ClubAccess(ClubDbContext db, CurrentUser current)
{
    public async Task<Horse> Horse(Guid id, bool allowArchived = false)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await db.Horses.FindAsync(id));
        if (horse.Archived && !allowArchived) throw new ApiException(409, "horse_archived", "Horse is archived.");
        if (user.Role == Role.ClubManager) return horse;
        if (user.Role == Role.HorseOwner && horse.OwnerId == user.Id) return horse;
        if (await db.Assignments.AnyAsync(x => x.HorseId == id && x.StaffId == user.Id && x.Active)) return horse;
        // Riders only see horses with sessions specifically assigned to them.
        if (user.Role == Role.WorkRider && await db.Sessions.AnyAsync(x => x.HorseId == id && x.RiderId == user.Id)) return horse;
        throw new ApiException(403, "forbidden", "Horse is outside your assigned scope.");
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
            "Only the current assigned Trainer may change training.", 403, "forbidden");
    }
    public async Task Vet(Guid horseId)
    {
        Ensure.Role(await current.Get(), Role.Veterinarian);
        await Horse(horseId);
    }
    public async Task<User> Staff(Guid id, Role role)
    {
        var user = Ensure.Found(await db.Users.FindAsync(id));
        Ensure.That(user.Active && user.Role == role, $"Staff must be an active {role}.");
        return user;
    }
    public async Task<HorseRegistration> Registration(Guid id)
    {
        var u = await current.Get();
        var r = Ensure.Found(await db.Registrations.FindAsync(id));
        Ensure.That(u.Role == Role.ClubManager || (u.Role == Role.HorseOwner && r.OwnerId == u.Id), "Permission denied.", 403, "forbidden");
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
    public void Notify(Guid recipient, NotificationType type, string message, Guid? reference = null)
        => db.Notifications.Add(new Notification { RecipientId = recipient, Type = type, Message = message, ReferenceId = reference });
    public async Task Managers(NotificationType type, string message, Guid reference)
    {
        foreach (var id in await db.Users.Where(x => x.Active && x.Role == Role.ClubManager).Select(x => x.Id).ToListAsync()) Notify(id, type, message, reference);
    }
    public async Task HorseStaff(Guid horseId, NotificationType type, string message, params Role[] roles)
    {
        foreach (var id in await db.Assignments.Where(x => x.HorseId == horseId && x.Active && roles.Contains(x.Role)).Select(x => x.StaffId).Distinct().ToListAsync())
            Notify(id, type, message, horseId);
    }
}

// One gate coordinates request transactions with background outbox/reminder workers.
// Serializable DB transactions protect checks/writes across processes; SQLite deployment should use one replica.
public sealed class WriteGate { public SemaphoreSlim Semaphore { get; } = new(1, 1); }
public sealed class TransactionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ClubDbContext db, WriteGate gate)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
        { await next(context); return; }
        await gate.Semaphore.WaitAsync(context.RequestAborted);
        var originalBody = context.Response.Body;
        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, context.RequestAborted);
            await next(context);
            if (context.Response.StatusCode < 400 || context.Items.ContainsKey("commit-auth-attempt")) await tx.CommitAsync(context.RequestAborted);
            else await tx.RollbackAsync(context.RequestAborted);
            context.Response.Body = originalBody;
            bufferedBody.Position = 0;
            await bufferedBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally { context.Response.Body = originalBody; gate.Semaphore.Release(); }
    }
}

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, code, message, reference) = ex switch
            {
                ApiException e => (e.Status, e.Code, e.Message, e.ReferenceId),
                DbUpdateConcurrencyException => (409, "concurrent_update", "Record changed. Reload and retry.", (Guid?)null),
                DbUpdateException => (409, "data_conflict", "Duplicate data or conflicting update.", (Guid?)null),
                BadHttpRequestException => (400, "invalid_request", "Invalid request body.", (Guid?)null),
                _ => (500, "internal_error", "Unexpected server error.", (Guid?)null)
            };
            if (status == 500) logger.LogError(ex, "Request failed {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { type = $"urn:horseclub:error:{code}", title = code, status, detail = message, referenceId = reference, traceId = context.TraceIdentifier });
        }
    }
}

public sealed class PageReader(IOptions<BusinessOptions> options)
{
    public async Task<object> Page<T>(IQueryable<T> q, int? page, int? pageSize)
    {
        var p = page ?? 1; var size = pageSize ?? options.Value.DefaultPageSize;
        Ensure.That(size >= 1 && size <= options.Value.MaxPageSize && p >= 1 && p <= int.MaxValue / size, $"page must be positive and pageSize between 1 and {options.Value.MaxPageSize}.");
        return new { items = await q.Skip((p - 1) * size).Take(size).ToListAsync(), page = p, pageSize = size, total = await q.CountAsync() };
    }
}
