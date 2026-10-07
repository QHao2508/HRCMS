using HorseClub.BLL.Contracts;

namespace Horse_BackEnd.Endpoints;

public static class IncidentPhotoEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module IncidentPhotos với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapIncidentPhotos.</param>
    public static void MapIncidentPhotos(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/care/incidents/{incidentId:guid}/photos").RequireAuthorization().WithTags("Incident photos");
        group.MapGet("", async (Guid incidentId, IncidentPhotoService moduleService) => await moduleService.ListPhotos(incidentId)).Produces<List<IncidentPhotoResponse>>(200);
        group.MapPost("", async (Guid incidentId, HttpRequest request, IncidentPhotoService moduleService) => (await moduleService.UploadPhoto(incidentId, UploadRequestAdapter.Create(request))).ToHttpResult()).RequireRateLimiting("uploads").Produces<IncidentPhotoCreatedResponse>(201);
        group.MapGet("/{id:guid}", async (Guid incidentId, Guid id, IncidentPhotoService moduleService) => (await moduleService.DownloadPhoto(incidentId, id)).ToHttpResult()).Produces<byte[]>(200, contentType: "application/octet-stream");
    }

}
