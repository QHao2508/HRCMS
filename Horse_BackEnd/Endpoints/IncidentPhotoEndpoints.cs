using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.Extensions.Options;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class IncidentPhotoEndpoints
{
    public static void MapIncidentPhotos(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/care/incidents/{incidentId:guid}/photos").RequireAuthorization().WithTags("Incident photos");
        group.MapGet("", async (Guid incidentId, ClubDbContext db, ClubAccess access, CurrentUser current) => await IncidentPhotoWorkflow.GetList(incidentId, db, access, current)).Produces<List<IncidentPhotoResponse>>(200);
        group.MapPost("", async (Guid incidentId, HttpRequest request, ClubDbContext db, ClubAccess access, CurrentUser current, ClubEvents events,
            IOptions<StorageOptions> limits, UploadStorage storage) => await IncidentPhotoWorkflow.Handle02(incidentId, request, db, access, current, events, limits, storage)).RequireRateLimiting("uploads").Produces<IncidentPhotoCreatedResponse>(201);
        group.MapGet("/{id:guid}", async (Guid incidentId, Guid id, ClubDbContext db, ClubAccess access, CurrentUser current, UploadStorage storage) => await IncidentPhotoWorkflow.GetById(incidentId, id, db, access, current, storage)).Produces<byte[]>(200, contentType: "application/octet-stream");
    }

}
