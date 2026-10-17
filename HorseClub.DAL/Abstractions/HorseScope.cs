namespace HorseClub.DAL.Abstractions;

/// <summary>Data constraints chosen by business authorization; all null means unrestricted non-archived horses.</summary>
public sealed record HorseScope(Guid? OwnerId = null, Guid? StaffId = null, Guid? RiderId = null);
