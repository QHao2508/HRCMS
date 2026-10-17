using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record InjuryRequest(Guid MedicalRecordId, DateOnly InjuryDate, [property: JsonRequired] InjuryType Type,
    [property: Required] string BodyLocation, [property: JsonRequired] InjurySeverity Severity, [property: Required] string Cause, DateOnly ReviewDate);
