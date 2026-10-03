using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class MedicalEndpoints
{
    public static void MapMedical(this RouteGroupBuilder api)
    {
        var m = api.MapGroup("/horses/{horseId:guid}/medical").WithTags("Medical").RequireAuthorization();
        m.MapGet("/summary", async (Guid horseId, ClubAccess access, ClubDbContext db, TimeProvider clock) => await MedicalWorkflow.GetSummary(horseId, access, db, clock));
        m.MapGet("/records", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetRecords(horseId, access, db, page, pageSize, pager));
        m.MapPost("/records", async (Guid horseId, MedicalRequest r, ClubAccess access, ClubDbContext db, CurrentUser current, ClubEvents events, TimeProvider clock) => await MedicalWorkflow.PostRecords(horseId, r, access, db, current, events, clock));
        m.MapGet("/injuries", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetInjuries(horseId, access, db, page, pageSize, pager));
        m.MapPut("/records/{id:guid}", async (Guid horseId, Guid id, MedicalRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock) => await MedicalWorkflow.PutRecordsById(horseId, id, r, access, current, db, events, clock));
        m.MapPost("/injuries", async (Guid horseId, InjuryRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar) => await MedicalWorkflow.PostInjuries(horseId, r, access, db, events, clock, calendar));
        m.MapGet("/restrictions", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetRestrictions(horseId, access, db, page, pageSize, pager));
        m.MapPost("/restrictions", async (Guid horseId, RestrictionRequest r, ClubAccess access, ClubDbContext db, ClubEvents events) => await MedicalWorkflow.PostRestrictions(horseId, r, access, db, events));
        m.MapGet("/treatments", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetTreatments(horseId, access, db, page, pageSize, pager));
        m.MapPost("/treatments", async (Guid horseId, TreatmentRequest r, ClubAccess access, ClubDbContext db, ClubEvents events) => await MedicalWorkflow.PostTreatments(horseId, r, access, db, events));
        m.MapGet("/follow-ups", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetFollowUps(horseId, access, db, page, pageSize, pager));
        m.MapPost("/follow-ups", async (Guid horseId, FollowUpRequest r, ClubAccess access, ClubDbContext db, CurrentUser current, ClubEvents events, TimeProvider clock) => await MedicalWorkflow.PostFollowUps(horseId, r, access, db, current, events, clock));
        m.MapGet("/preventive-care", async (Guid horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await MedicalWorkflow.GetPreventiveCare(horseId, access, db, page, pageSize, pager));
        m.MapPost("/preventive-care", async (Guid horseId, PreventiveRequest r, ClubAccess access, ClubDbContext db, ClubEvents events) => await MedicalWorkflow.PostPreventiveCare(horseId, r, access, db, events));
        m.MapPost("/preventive-care/{id:guid}/complete", async (Guid horseId, Guid id, PreventiveCompletionRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar) => await MedicalWorkflow.PostPreventiveCareByIdComplete(horseId, id, r, access, db, events, clock, calendar));
    }

}
