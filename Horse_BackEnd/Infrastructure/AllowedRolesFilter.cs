using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Infrastructure;

public sealed class AllowedRolesFilter(params Role[] roles) : IEndpointFilter
{
    /// <summary>
    /// Kiểm tra vai trò cho endpoint có giới hạn role, chặn request không đủ quyền trước khi gọi handler.
    /// </summary>
    /// <param name="context">Giá trị kiểu EndpointFilterInvocationContext dùng trong InvokeAsync.</param>
    /// <param name="next">Giá trị kiểu EndpointFilterDelegate dùng trong InvokeAsync.</param>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await context.HttpContext.RequestServices.GetRequiredService<CurrentUser>().Get();
        Ensure.Role(user, roles);
        return await next(context);
    }
}
