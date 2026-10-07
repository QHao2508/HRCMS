using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;

namespace Horse_BackEnd.Endpoints;

public static class MedicalEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Medical với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapMedical.</param>
    public static void MapMedical(this RouteGroupBuilder api)
    {
        var m = api.MapGroup("/horses/{horseId:guid}/medical").WithTags("Medical").RequireAuthorization();
        m.MapGet("/summary", async (Guid horseId, MedicalService moduleService) => await moduleService.GetSummary(horseId)).Produces<MedicalSummaryResponse>(200);
        m.MapGet("/records", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListExaminations(horseId, page, pageSize)).Produces<PageResponse<MedicalRecord>>(200);
        m.MapPost("/records", async (Guid horseId, MedicalRequest r, MedicalService moduleService) => (await moduleService.CreateExamination(horseId, r)).ToHttpResult()).Produces<MedicalRecord>(201);
        m.MapGet("/injuries", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListInjuries(horseId, page, pageSize)).Produces<PageResponse<Injury>>(200);
        m.MapPut("/records/{id:guid}", async (Guid horseId, Guid id, MedicalRequest r, MedicalService moduleService) => await moduleService.CorrectExamination(horseId, id, r)).Produces<MedicalRecord>(200);
        m.MapPost("/injuries", async (Guid horseId, InjuryRequest r, MedicalService moduleService) => await moduleService.RecordInjury(horseId, r)).Produces<Injury>(200);
        m.MapGet("/restrictions", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListRestrictions(horseId, page, pageSize)).Produces<PageResponse<MedicalRestriction>>(200);
        m.MapPost("/restrictions", async (Guid horseId, RestrictionRequest r, MedicalService moduleService) => await moduleService.CreateRestriction(horseId, r)).Produces<MedicalRestriction>(200);
        m.MapGet("/treatments", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListTreatments(horseId, page, pageSize)).Produces<PageResponse<TreatmentPlan>>(200);
        m.MapPost("/treatments", async (Guid horseId, TreatmentRequest r, MedicalService moduleService) => await moduleService.CreateTreatment(horseId, r)).Produces<TreatmentPlan>(200);
        m.MapGet("/follow-ups", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListFollowUps(horseId, page, pageSize)).Produces<PageResponse<MedicalFollowUp>>(200);
        m.MapPost("/follow-ups", async (Guid horseId, FollowUpRequest r, MedicalService moduleService) => await moduleService.RecordFollowUp(horseId, r)).Produces<MedicalFollowUp>(200);
        m.MapGet("/preventive-care", async (Guid horseId, int? page, int? pageSize, MedicalService moduleService) => await moduleService.ListPreventiveCare(horseId, page, pageSize)).Produces<PageResponse<PreventiveCareSummaryResponse>>(200);
        m.MapPost("/preventive-care", async (Guid horseId, PreventiveRequest r, MedicalService moduleService) => await moduleService.SchedulePreventiveCare(horseId, r)).Produces<PreventiveCare>(200);
        m.MapPost("/preventive-care/{id:guid}/complete", async (Guid horseId, Guid id, PreventiveCompletionRequest r, MedicalService moduleService) => (await moduleService.CompletePreventiveCare(horseId, id, r)).ToHttpResult()).Produces(204);
    }

}
