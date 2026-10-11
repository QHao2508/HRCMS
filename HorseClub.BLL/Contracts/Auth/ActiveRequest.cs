using System.Text.Json.Serialization;

namespace HorseClub.BLL.Contracts;

public sealed record ActiveRequest([property: JsonRequired] bool Active, bool Emergency = false, [property: System.ComponentModel.DataAnnotations.MaxLength(2000)] string? Reason = null);
