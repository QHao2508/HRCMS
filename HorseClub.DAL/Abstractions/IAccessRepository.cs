namespace HorseClub.DAL.Abstractions;

public interface IAccessRepository
{
    ValueTask<User?> FindUserAsync(Guid id);
    ValueTask<HorseRegistration?> FindRegistrationAsync(Guid id);
    Task<bool> HasRiderSessionAsync(Guid horseId, Guid riderId);
    Task<bool> HasHistoricalAssignmentAsync(Guid horseId, Guid staffId);
}
