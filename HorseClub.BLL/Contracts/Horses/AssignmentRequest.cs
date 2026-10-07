using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record AssignmentRequest(Guid StaffId, [property: JsonRequired] Role Role, DateOnly StartDate, [property: MaxLength(2000)] string Notes);
