using HorseClub.BLL.Contracts;

namespace Horse_BackEnd.Endpoints;

public static class AttachmentEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Attachments với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapAttachments.</param>
    public static void MapAttachments(this RouteGroupBuilder api)
    {
        // Public profile images are still protected by the horse scope; staff cannot access intake medical files.
        api.MapGet("/horses/{horseId:guid}/photo", async (Guid horseId, AttachmentService moduleService) => (await moduleService.GetHorsePhoto(horseId)).ToHttpResult()).RequireAuthorization().WithTags("Attachments").Produces<byte[]>(200, contentType: "application/octet-stream");
        var a = api.MapGroup("/registrations/{registrationId:guid}/attachments").WithTags("Attachments").RequireAuthorization();
        a.MapGet("", async (Guid registrationId, AttachmentService moduleService) => await moduleService.ListAttachments(registrationId)).Produces<List<AttachmentResponse>>(200);
        a.MapPost("", async (Guid registrationId, HttpRequest request, AttachmentService moduleService) => (await moduleService.UploadAttachment(registrationId, UploadRequestAdapter.Create(request))).ToHttpResult()).RequireRateLimiting("uploads").Produces<AttachmentCreatedResponse>(201);
        a.MapGet("/{id:guid}", async (Guid registrationId, Guid id, AttachmentService moduleService) => (await moduleService.DownloadAttachment(registrationId, id)).ToHttpResult()).Produces<byte[]>(200, contentType: "application/octet-stream");
    }

}
