using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;
namespace HorseClub.BLL.Contracts;
public sealed record HorseArchiveRequest([property: JsonRequired] HorseArchiveCategory Category, [property: Required, MaxLength(2000)] string Reason);
