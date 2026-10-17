namespace Horse_BackEnd.Infrastructure;

public sealed class ValidationFilter : IEndpointFilter
{
    /// <summary>
    /// Kiểm tra DataAnnotations và cấu trúc request trước khi endpoint thực hiện nghiệp vụ; trả lỗi validation khi dữ liệu không hợp lệ.
    /// </summary>
    /// <param name="context">Giá trị kiểu EndpointFilterInvocationContext dùng trong InvokeAsync.</param>
    /// <param name="next">Giá trị kiểu EndpointFilterDelegate dùng trong InvokeAsync.</param>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
            if (arg?.GetType().Namespace == "HorseClub.BLL.Contracts") Ensure.Validate(arg);
        return await next(context);
    }
}
