using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record PreventiveRequest([property: JsonRequired] PreventiveCareType Type, DateOnly DueDate, string Notes);
