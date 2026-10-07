using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record PlanStatusRequest([property: JsonRequired] PlanStatus Status);
