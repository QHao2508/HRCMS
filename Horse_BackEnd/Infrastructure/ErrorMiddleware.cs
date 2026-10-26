using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Infrastructure;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    /// <summary>
    /// Chuyển lỗi nghiệp vụ, validation, trùng dữ liệu và lỗi hệ thống thành phản hồi thống nhất; giữ chi tiết nội bộ khỏi response client.
    /// Client hiển thị theo messageKey/args; detail tiếng Anh chỉ để log và debug.
    /// </summary>
    /// <param name="context">Giá trị kiểu HttpContext dùng trong InvokeAsync.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, code, message, reference) = ex switch
            {
                ApiException e => (e.Status, e.Code, new MessageText(e.Key, e.Args), e.ReferenceId),
                _ when DatabaseFailures.IsDeadlock(ex) => (409, "transaction_conflict", Messages.Text(MessageKey.RecordChangedReloadAndRetry), (Guid?)null),
                DbUpdateConcurrencyException => (409, "concurrent_update", Messages.Text(MessageKey.RecordChangedReloadAndRetry), (Guid?)null),
                DbUpdateException => (409, "data_conflict", Messages.Text(MessageKey.DuplicateDataOrConflictingUpdate), (Guid?)null),
                BadHttpRequestException => (400, "invalid_request", Messages.Text(MessageKey.InvalidRequestBody), (Guid?)null),
                _ => (500, "internal_error", Messages.Text(MessageKey.UnexpectedServerError), (Guid?)null)
            };
            if (status == 500) logger.LogError(ex, "Request failed {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse($"urn:horseclub:error:{code}", code, status, message.ToString(), reference, context.TraceIdentifier, message.Key.ToString(), message.Args));
        }
    }
}
