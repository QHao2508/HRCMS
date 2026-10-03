using HorseClub.BLL.Messaging;
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
                DbUpdateConcurrencyException => (409, "concurrent_update", Messages.Get(MessageKey.RecordChangedReloadAndRetry), (Guid?)null),
                DbUpdateException => (409, "data_conflict", Messages.Get(MessageKey.DuplicateDataOrConflictingUpdate), (Guid?)null),
                BadHttpRequestException => (400, "invalid_request", Messages.Get(MessageKey.InvalidRequestBody), (Guid?)null),
                _ => (500, "internal_error", Messages.Get(MessageKey.UnexpectedServerError), (Guid?)null)
            };
            if (status == 500) logger.LogError(ex, "Request failed {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { type = $"urn:horseclub:error:{code}", title = code, status, detail = message, referenceId = reference, traceId = context.TraceIdentifier });
        }
    }
}
