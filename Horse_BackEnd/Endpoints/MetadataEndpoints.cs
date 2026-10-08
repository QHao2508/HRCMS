namespace Horse_BackEnd.Endpoints;

public static class MetadataEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Metadata với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapMetadata.</param>
    public static void MapMetadata(this RouteGroupBuilder api)
    {
        api.MapGet("/metadata/enums", async (IMetadataService moduleService) => await moduleService.GetEnums()).RequireAuthorization().WithTags("Metadata").Produces<Dictionary<string, string[]>>(200);
    }
}
