using System.Data;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Infrastructure;

public sealed class TransactionMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Bao các request ghi trong transaction Serializable và buffer response; commit thành công hoặc lượt thử đăng nhập, rollback lỗi và dọn blob chưa được commit.
    /// </summary>
    /// <param name="context">Giá trị kiểu HttpContext dùng trong InvokeAsync.</param>
    /// <param name="db">Giá trị kiểu ClubDbContext dùng trong InvokeAsync.</param>
    /// <param name="uploads">Giá trị kiểu UploadStorage dùng trong InvokeAsync.</param>
    /// <remarks>Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.</remarks>
    public async Task InvokeAsync(HttpContext context, ClubDbContext db, UploadStorage uploads)
    {
        if (context.GetEndpoint() is null || context.GetEndpoint()?.Metadata.GetMetadata<ReadOnlyOperation>() is not null
            || HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
        { await next(context); return; }
        var originalBody = context.Response.Body;
        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, context.RequestAborted);
            await next(context);
            if (context.Response.StatusCode < 400 || context.Items.ContainsKey("commit-auth-attempt"))
            { await tx.CommitAsync(context.RequestAborted); uploads.Commit(); }
            else await tx.RollbackAsync(context.RequestAborted);
            context.Response.Body = originalBody;
            bufferedBody.Position = 0;
            await bufferedBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
            await uploads.CleanupUncommitted();
        }
    }
}
