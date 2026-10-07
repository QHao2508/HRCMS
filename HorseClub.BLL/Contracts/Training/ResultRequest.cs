using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record ResultRequest([property: Range(0, 100000)] decimal DistanceMetres,
    [property: Range(0.001, 86400)] decimal TimeSeconds, [property: Range(1, 300)] int? HeartRate,
    [property: JsonRequired] Intensity Intensity, [property: Required] string Feedback, bool AbnormalObservation);
