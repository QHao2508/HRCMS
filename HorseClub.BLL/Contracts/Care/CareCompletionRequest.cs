using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record CareCompletionRequest([property: JsonRequired] CareStatus Status, [property: Range(0, 100)] decimal? ActualPortionKg, [property: Required] string Notes);
