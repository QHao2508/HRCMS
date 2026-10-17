using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record CareRequest(Guid HorseId, Guid GroomId, Guid? TreatmentPlanId, [property: JsonRequired] CareType Type,
    DateTimeOffset ScheduledAt, [property: Required] string Instructions, [property: Range(0, 100)] decimal? ApprovedPortionKg);
