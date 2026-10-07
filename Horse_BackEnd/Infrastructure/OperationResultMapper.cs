namespace Horse_BackEnd.Infrastructure;

/// <summary>Keeps ASP.NET response construction outside business services.</summary>
public static class OperationResultMapper
{
    /// <summary>
    /// Chuyển kết quả thuần nghiệp vụ thành JSON, Created, NoContent hoặc file stream tại layer API.
    /// </summary>
    /// <param name="result">Giá trị kiểu OperationResult dùng trong ToHttpResult.</param>
    public static IResult ToHttpResult(this OperationResult result)
    {
        if (result.Download is { } download)
            return Results.File(download.Content, download.ContentType, download.FileName);
        if (result.StatusCode == 201) return Results.Created(result.Location, result.Value);
        if (result.StatusCode == 204) return Results.NoContent();
        return Results.Json(result.Value, statusCode: result.StatusCode);
    }
}
