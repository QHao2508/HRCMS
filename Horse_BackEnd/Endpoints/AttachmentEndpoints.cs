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
        api.MapGet("/horses/{horseId:guid}/photo", async (Guid horseId, ClubAccess access, ClubDbContext db, IConfiguration config, IWebHostEnvironment env) => await AttachmentWorkflow.GetHorsesByIdPhoto(horseId, access, db, config, env)).RequireAuthorization().WithTags("Attachments");
        var a = api.MapGroup("/registrations/{registrationId:guid}/attachments").WithTags("Attachments").RequireAuthorization();
        a.MapGet("", async (Guid registrationId, ClubAccess access, ClubDbContext db) => await AttachmentWorkflow.GetList(registrationId, access, db));
        a.MapPost("", async (Guid registrationId, HttpRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, IConfiguration config, IWebHostEnvironment env, ClubEvents events, IOptions<StorageOptions> options) => await AttachmentWorkflow.PostList(registrationId, request, access, current, db, config, env, events, options)).RequireRateLimiting("uploads");
        a.MapGet("/{id:guid}", async (Guid registrationId, Guid id, ClubAccess access, ClubDbContext db, IConfiguration config, IWebHostEnvironment env) => await AttachmentWorkflow.GetById(registrationId, id, access, db, config, env));
    }

}
