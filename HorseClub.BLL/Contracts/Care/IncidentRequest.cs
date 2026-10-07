using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record IncidentRequest(Guid HorseId, DateTimeOffset OccurredAt, [property: JsonRequired] IncidentType Type,
    [property: Required] string Description, [property: JsonRequired] IncidentSeverity Severity, [property: JsonRequired] Role RoutedTo);
