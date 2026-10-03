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
        group.MapGet("", async (Guid incidentId, ClubDbContext db, ClubAccess access, CurrentUser current) => await IncidentPhotoWorkflow.GetList(incidentId, db, access, current));
        group.MapPost("", async (Guid incidentId, HttpRequest request, ClubDbContext db, ClubAccess access, CurrentUser current, ClubEvents events,
            IOptions<StorageOptions> limits, IConfiguration config, IWebHostEnvironment env) => await IncidentPhotoWorkflow.Handle02(incidentId, request, db, access, current, events, limits, config, env)).RequireRateLimiting("uploads");
        group.MapGet("/{id:guid}", async (Guid incidentId, Guid id, ClubDbContext db, ClubAccess access, CurrentUser current, IConfiguration config, IWebHostEnvironment env) => await IncidentPhotoWorkflow.GetById(incidentId, id, db, access, current, config, env));
    }

}
