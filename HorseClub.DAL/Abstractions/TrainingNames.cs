namespace HorseClub.DAL.Abstractions;
public sealed record TrainingNames(Guid Id, string HorseName, string TrainerName, string? RiderName = null);
