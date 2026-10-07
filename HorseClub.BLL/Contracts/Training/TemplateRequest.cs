using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record TemplateRequest([property: Required, MaxLength(200)] string Name, [property: Required] string Goal,
    [property: Required] string Phase, [property: Range(1, 100000)] decimal DistanceMetres, [property: JsonRequired] Intensity Intensity,
    [property: Required] string Surface, [property: Range(1, 21)] int FrequencyPerWeek, string Notes);
