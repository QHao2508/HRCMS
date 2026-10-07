using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record PlanRequest(Guid HorseId, Guid TemplateId, [property: Required] string Goal, [property: Required] string Phase,
    DateOnly StartDate, DateOnly EndDate, string Notes);
