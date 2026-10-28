using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HorseClub.DAL.Repositories;

public sealed class RegistrationRepository(ClubDbContext db) : IRegistrationRepository
{
    public void Add(HorseRegistration registration) => db.Registrations.Add(registration);
    public void AddApprovedHorse(Horse horse, Measurement measurement)
    { db.Horses.Add(horse); db.Measurements.Add(measurement); }
    public async Task<string> NextHorseRegistrationNumberAsync()
    {
        while (true)
        {
            var code = HorseRegistrationNumbers.Format(await NextSequenceValueAsync());
            // Bỏ qua số đã bị một mã nhập tay cũ chiếm (dữ liệu trước khi có mã tự cấp).
            if (!await db.Horses.AnyAsync(h => h.RegistrationNumber == code)) return code;
        }
    }

    // NEXT VALUE FOR không được đặt trong subquery, nên chạy lệnh trực tiếp thay vì để EF bọc truy vấn.
    private async Task<long> NextSequenceValueAsync()
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [" + HorseRegistrationNumbers.Sequence + "]";
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
    public Task<bool> HasAttachmentAsync(Guid registrationId, AttachmentType type)
        => db.Attachments.AnyAsync(x => x.RegistrationId == registrationId && x.Type == type);
    public Task<Dictionary<Guid, string>> OwnerNamesAsync(IEnumerable<Guid> ownerIds)
        => db.Users.AsNoTracking().Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim());
    public async Task<DataPage<HorseRegistration>> ListAsync(Guid? ownerId, RegistrationStatus? status, int page, int size)
    {
        var query = db.Registrations.AsNoTracking();
        if (ownerId.HasValue) query = query.Where(x => x.OwnerId == ownerId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        return await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToPageAsync(page, size);
    }
}
