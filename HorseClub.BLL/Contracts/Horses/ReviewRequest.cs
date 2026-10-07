using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HorseClub.BLL.Contracts;

public sealed record ReviewRequest([property: JsonRequired] bool Approve, [property: MaxLength(2000)] string? Reason);
