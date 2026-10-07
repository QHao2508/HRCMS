using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record MeasurementRequest(DateOnly Date, [property: Range(1, 300)] decimal HeightCm, [property: Range(1, 2000)] decimal WeightKg);
