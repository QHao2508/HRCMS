using System.Text.Json.Serialization;

namespace HorseClub.BLL.Contracts;

public sealed record ActiveRequest([property: JsonRequired] bool Active);
