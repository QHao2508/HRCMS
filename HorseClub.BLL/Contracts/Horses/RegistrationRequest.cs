using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record RegistrationRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(200)] string Sire,
    [property: Required, MaxLength(200)] string Dam,
    DateOnly DateOfBirth, [property: JsonRequired] HorseGender Gender,
    [property: Required, MaxLength(100)] string Breed, [property: MaxLength(100)] string? RegistrationNumber,
    [property: Range(1, 300)] decimal HeightCm, [property: Range(1, 2000)] decimal WeightKg, DateOnly MeasurementDate,
    [property: Required, MaxLength(1000)] string DeclaredHealth, [property: MaxLength(2000)] string HealthNotes,
    DateOnly BoardingStart, DateOnly? BoardingEnd, Guid? PreferredHeadTrainerId, Guid? PreferredGroomId, Guid? PreferredVeterinarianId);
