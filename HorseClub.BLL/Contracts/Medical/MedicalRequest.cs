using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record MedicalRequest(DateTimeOffset ExaminationAt, [property: Required] string Reason,
    [property: Required] string Symptoms, [property: Required] string Findings, [property: Required] string Diagnosis,
    [property: JsonRequired] HealthStatus HealthStatus, string Notes);
