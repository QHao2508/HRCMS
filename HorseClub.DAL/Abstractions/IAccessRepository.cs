namespace HorseClub.DAL.Abstractions;

public interface IAccessRepository
{
    ValueTask<User?> FindUserAsync(Guid id);
    ValueTask<HorseRegistration?> FindRegistrationAsync(Guid id);
    Task<bool> HasRiderSessionAsync(Guid horseId, Guid riderId);
}
