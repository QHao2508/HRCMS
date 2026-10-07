using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.Extensions.Options;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class AttachmentEndpoints
{
    public static void MapAttachments(this RouteGroupBuilder api)
    {
        // Public profile images are still protected by the horse scope; staff cannot access intake medical files.
        api.MapGet("/horses/{horseId:guid}/photo", async (Guid horseId, ClubAccess access, ClubDbContext db, UploadStorage storage) => await AttachmentWorkflow.GetHorsesByIdPhoto(horseId, access, db, storage)).RequireAuthorization().WithTags("Attachments").Produces<byte[]>(200, contentType: "application/octet-stream");
        var a = api.MapGroup("/registrations/{registrationId:guid}/attachments").WithTags("Attachments").RequireAuthorization();
        a.MapGet("", async (Guid registrationId, ClubAccess access, ClubDbContext db) => await AttachmentWorkflow.GetList(registrationId, access, db)).Produces<List<AttachmentResponse>>(200);
        a.MapPost("", async (Guid registrationId, HttpRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, UploadStorage storage, ClubEvents events, IOptions<StorageOptions> options) => await AttachmentWorkflow.PostList(registrationId, request, access, current, db, storage, events, options)).RequireRateLimiting("uploads").Produces<AttachmentCreatedResponse>(201);
        a.MapGet("/{id:guid}", async (Guid registrationId, Guid id, ClubAccess access, ClubDbContext db, UploadStorage storage) => await AttachmentWorkflow.GetById(registrationId, id, access, db, storage)).Produces<byte[]>(200, contentType: "application/octet-stream");
    }

}
