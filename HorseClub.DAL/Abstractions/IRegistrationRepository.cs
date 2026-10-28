namespace HorseClub.DAL.Abstractions;

public interface IRegistrationRepository
{
    void Add(HorseRegistration registration);
    void AddApprovedHorse(Horse horse, Measurement measurement);
    /// <summary>Cấp mã đăng ký ngựa mới, chưa ngựa nào dùng.</summary>
    Task<string> NextHorseRegistrationNumberAsync();
    Task<bool> HasAttachmentAsync(Guid registrationId, AttachmentType type);
    Task<DataPage<HorseRegistration>> ListAsync(Guid? ownerId, RegistrationStatus? status, int page, int size);
    Task<Dictionary<Guid, string>> OwnerNamesAsync(IEnumerable<Guid> ownerIds);
}
