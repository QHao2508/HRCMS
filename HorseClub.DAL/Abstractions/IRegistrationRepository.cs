namespace HorseClub.DAL.Abstractions;

public interface IRegistrationRepository
{
    void Add(HorseRegistration registration);
    void AddApprovedHorse(Horse horse, Measurement measurement);
    Task<bool> HasAttachmentAsync(Guid registrationId, AttachmentType type);
    Task<DataPage<HorseRegistration>> ListAsync(Guid? ownerId, RegistrationStatus? status, int page, int size);
}
