namespace Horse_BackEnd.Endpoints;

public static class BrandingEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Branding với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapBranding.</param>
    public static void MapBranding(this RouteGroupBuilder api)
    {
        api.MapGet("/branding/logo", async (IBrandingService branding, HttpContext context, CancellationToken token) =>
        {
            var stream = await branding.OpenLogo(token);
            context.Response.Headers.CacheControl = "public,max-age=86400";
            return Results.Stream(stream, "image/png");
        }).AllowAnonymous().WithTags("Branding").Produces<byte[]>(200, contentType: "image/png");
    }
}
