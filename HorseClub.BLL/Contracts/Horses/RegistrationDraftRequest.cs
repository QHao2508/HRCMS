using System.ComponentModel.DataAnnotations;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

// PUT replaces the Owner's draft. Missing intake fields remain null until submission.
public sealed record RegistrationDraftRequest(
    [property: MaxLength(200)] string? Name = null,
    [property: MaxLength(200)] string? Sire = null,
    [property: MaxLength(200)] string? Dam = null,
    DateOnly? DateOfBirth = null, HorseGender? Gender = null,
    [property: MaxLength(100)] string? Breed = null, [property: MaxLength(100)] string? RegistrationNumber = null,
    decimal? HeightCm = null, decimal? WeightKg = null, DateOnly? MeasurementDate = null,
    [property: MaxLength(1000)] string? DeclaredHealth = null, [property: MaxLength(2000)] string? HealthNotes = null,
    DateOnly? BoardingStart = null, DateOnly? BoardingEnd = null,
    Guid? PreferredHeadTrainerId = null, Guid? PreferredGroomId = null, Guid? PreferredVeterinarianId = null);
