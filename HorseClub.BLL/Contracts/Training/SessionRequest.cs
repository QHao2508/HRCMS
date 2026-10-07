using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record SessionRequest(DateTimeOffset ScheduledAt, [property: JsonRequired] TrainingType TrainingType,
    [property: Range(1, 100000)] decimal DistanceMetres, [property: JsonRequired] Intensity Intensity, [property: Required] string Surface,
    [property: Required] string Target, string Notes, Guid? RiderId);
