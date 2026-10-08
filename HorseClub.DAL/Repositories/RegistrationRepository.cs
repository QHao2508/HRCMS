using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class RegistrationRepository(ClubDbContext db) : IRegistrationRepository
{
    public void Add(HorseRegistration registration) => db.Registrations.Add(registration);
    public void AddApprovedHorse(Horse horse, Measurement measurement)
    { db.Horses.Add(horse); db.Measurements.Add(measurement); }
    public Task<bool> HasAttachmentAsync(Guid registrationId, AttachmentType type)
        => db.Attachments.AnyAsync(x => x.RegistrationId == registrationId && x.Type == type);
    public async Task<DataPage<HorseRegistration>> ListAsync(Guid? ownerId, RegistrationStatus? status, int page, int size)
    {
        var query = db.Registrations.AsNoTracking();
        if (ownerId.HasValue) query = query.Where(x => x.OwnerId == ownerId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        return new(await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    }
}
